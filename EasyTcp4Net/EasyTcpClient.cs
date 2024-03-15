using CommunityToolkit.HighPerformance.Buffers;
using Microsoft.Extensions.Logging;
using System.Net;
using System.Net.NetworkInformation;
using System.Net.Security;
using System.Net.Sockets;
using System.Security.Cryptography.X509Certificates;

namespace EasyTcp4Net
{
    /// <summary>
    /// tcp 客户端类
    /// </summary>
    public class EasyTcpClient
    {
        public bool IsConnected { get; private set; } //客户端是否已经连接上服务
        public event EventHandler<ClientDataReceiveEventArgs> OnReceivedData;
        public event EventHandler<ClientSideDisConnectEventArgs> OnDisConnected;
        private readonly IPAddress _serverIpAddress = null; //服务端的ip地址
        private readonly Socket _socket;  //客户端本地套接字
        private readonly EasyTcpClientOptions _options = new();//客户端总配置
        private readonly X509Certificate2 _certificate;//ssl证书对象
        private readonly SemaphoreSlim _connectLock = new SemaphoreSlim(1, 1); //开启连接的信号量
        private readonly SemaphoreSlim _sendLock = new SemaphoreSlim(1, 1); //发送数据的信号量
        private readonly CancellationTokenSource _lifecycleTokenSource; //整个客户端存活的token
        private CancellationTokenSource _receiveDataTokenSource;//客户端获取数据的token
        private readonly IPEndPoint _remoteEndPoint; //服务端的终结点
        private readonly IPEndPoint _localEndPoint;
        private readonly ILogger<EasyTcpClient> _logger; //日志对象

        private Task _dataReceiveTask = null;
        private NetworkStream _networkStream;
        private SslStream _sslStream;

        /// <summary>
        /// 创建一个Tcp服务对象
        /// </summary>
        /// <param name="port">监听的端口</param>
        /// <param name="host">
        /// 监听的host
        /// 1.null or string.empty 默认监听所有的网卡地址
        /// 2.如果是域名，转换为ip地址
        /// </param>
        public EasyTcpClient(string serverHost, ushort serverPort)
        {
            if (string.IsNullOrEmpty(serverHost))
            {
                throw new ArgumentNullException(nameof(serverHost));
            }

            if (serverPort > 65535 || serverPort <= 0)
            {
                throw new InvalidDataException("Server port is invalid.");
            }

            if (serverHost.Trim() == "*")
            {
                _serverIpAddress = IPAddress.Any;
            }
            else
            {
                _serverIpAddress = IPAddress.TryParse(serverHost, out var tempAddress) ?
                    tempAddress : Dns.GetHostEntry(serverHost).AddressList[0];
            }
            _socket = new Socket(IPAddress.Any.AddressFamily, SocketType.Stream, ProtocolType.Tcp);
            _localEndPoint = new IPEndPoint(IPAddress.Any, 0);
            _remoteEndPoint = new IPEndPoint(_serverIpAddress, serverPort);
            _lifecycleTokenSource = new CancellationTokenSource();
            IsConnected = false;
        }

        /// <summary>
        /// 创建一个Tcp服务对象
        /// </summary>
        /// <param name="options">服务器对象配置</param>
        /// <exception cref="ArgumentNullException"></exception>
        public EasyTcpClient(string serverHost, ushort serverPort, EasyTcpClientOptions options) : this(serverHost, serverPort)
        {
            _options = options;
            if (_options.IsSsl)
            {
                if (string.IsNullOrEmpty(_options.PfxCertFilename))
                    throw new ArgumentNullException(nameof(_options.PfxCertFilename));

                if (string.IsNullOrEmpty(_options.PfxPassword))
                {
                    _certificate = new X509Certificate2(_options.PfxCertFilename);
                }
                else
                {
                    _certificate = new X509Certificate2(_options.PfxCertFilename, _options.PfxPassword);
                }
            }

            if (_options.KeepAlive) _socket.SetKeepAlive(_options.KeepAliveIntvl,_options.KeepAliveTime,_options.KeepAliveProbes);
            _socket.NoDelay = _options.NoDelay;
            _logger = options.LoggerFactory?.CreateLogger<EasyTcpClient>();
        }

        /// <summary>
        /// 客户端连接服务端
        /// </summary>
        /// <returns></returns>
        /// <exception cref="InvalidOperationException">连接错误</exception>
        /// <exception cref="TimeoutException">连接超时</exception>
        public async Task ConnectAsync()
        {
            _connectLock.Wait();
            try
            {
                if (IsConnected)
                    return;

                CancellationTokenSource connectTokenSource = new CancellationTokenSource();
                connectTokenSource.CancelAfter(_options.ConnectionTimeout);
                await _socket.ConnectAsync(_remoteEndPoint, connectTokenSource.Token);
                _networkStream = new NetworkStream(_socket);
                _networkStream.ReadTimeout = _options.ReadTimeout;
                _networkStream.WriteTimeout = _options.WriteTimeout;
                if (_options.IsSsl)
                {
                    if (_options.AllowingUntrustedSSLCertificate)
                    {
                        _sslStream = new SslStream(_networkStream, false,
                                (obj, certificate, chain, error) => true);
                    }
                    else
                    {
                        _sslStream = new SslStream(_networkStream, false);
                    }

                    _sslStream.ReadTimeout = _options.ReadTimeout;
                    _sslStream.WriteTimeout = _options.WriteTimeout;
                    await _sslStream.AuthenticateAsClientAsync(new SslClientAuthenticationOptions()
                    {
                        TargetHost = _remoteEndPoint.Address.ToString(),
                        EnabledSslProtocols = System.Security.Authentication.SslProtocols.Tls12,
                        CertificateRevocationCheckMode = _options.CheckCertificateRevocation ? X509RevocationMode.Online : X509RevocationMode.NoCheck,
                        ClientCertificates = new X509CertificateCollection() { _certificate }
                    }, connectTokenSource.Token).ConfigureAwait(false);
                    if (!_sslStream.IsEncrypted || !_sslStream.IsAuthenticated ||
                        (_options.MutuallyAuthenticate && !_sslStream.IsMutuallyAuthenticated))
                    {
                        throw new InvalidOperationException("SSL authenticated faild!");
                    }

                    //TODO keepalive
                }
            }
            catch (TaskCanceledException)
            {
                throw new TimeoutException("Connect remote host was canceled because of timeout !");
            }
            catch (OperationCanceledException)
            {
                throw new TimeoutException("Connect remote host timeout!");
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex.ToString());
                throw;
            }
            finally
            {
                _connectLock.Release();
            }

            _receiveDataTokenSource = new CancellationTokenSource();
            IsConnected = true;
            CancellationTokenSource ctx = CancellationTokenSource.CreateLinkedTokenSource(_lifecycleTokenSource.Token, _receiveDataTokenSource.Token);
            _dataReceiveTask = Task.Factory.StartNew(ReceiveDataAsync,
                ctx.Token, TaskCreationOptions.LongRunning, TaskScheduler.Current);
        }

        /// <summary>
        /// 客户端循环读取数据
        /// </summary>
        /// <returns></returns>
        private async Task ReceiveDataAsync()
        {
            while (!_receiveDataTokenSource.Token.IsCancellationRequested)
            {
                try
                {
                    var data = await DataReadAsync().ConfigureAwait(false);
                    if (!data.IsEmpty)
                    {
                        OnReceivedData?.Invoke(this, new ClientDataReceiveEventArgs(data));
                    }
                    else
                    {
                        await Task.Delay(20);
                    }
                }
                catch (IOException)
                {
                    //TODO log
                    break;
                }
                catch (SocketException)
                {
                    //TODO log
                    break;
                }
                catch (TaskCanceledException)
                {
                    //TODO log
                    break;
                }

            }
        }

        /// <summary>
        /// 读取数据
        /// </summary>
        /// <returns></returns>
        private async Task<Memory<byte>> DataReadAsync()
        {
            try
            {
                MemoryOwner<byte> buffer = MemoryOwner<byte>.Allocate(_options.BufferSize);
                int readCount = 0;
                if (_options.IsSsl)
                {
                    readCount = await _sslStream.ReadAsync(buffer.Memory, _lifecycleTokenSource.Token).ConfigureAwait(false);
                }
                else
                {
                    readCount = await _networkStream.ReadAsync(buffer.Memory, _lifecycleTokenSource.Token).ConfigureAwait(false);
                }

                if (readCount > 0)
                {
                    var data = buffer.Slice(0, readCount);
                    return data.Memory;
                }
                else
                {
                    if (IsDisconnect())
                    {
                        DisConnect();
                    }

                    throw new SocketException();
                }
            }
            catch (SocketException)
            {
                _logger?.LogError("Connection was disconnected.");
                throw;
            }
            catch (IOException)
            {
                throw;
            }
        }

        public async Task SendAsync(byte[] data)
        {
            if (data == null || data.Length < 1)
                throw new ArgumentNullException(nameof(data));

            if (!IsConnected) throw new InvalidOperationException("Connection is disconnected");
            await SendInternalAsync(data);
        }

        public async Task SendAsync(Memory<byte> data)
        {
            if (data.IsEmpty || data.Length < 1)
                throw new ArgumentNullException(nameof(data));

            if (!IsConnected) throw new InvalidOperationException("Connection is disconnected");
            await SendInternalAsync(data);
        }

        private async Task SendInternalAsync(Memory<byte> data)
        {
            int bytesRemaining = data.Length;
            int index = 0;

            try
            {
                _sendLock.Wait();
                while (bytesRemaining > 0)
                {
                    Memory<byte> needSendData = null;
                    if (bytesRemaining >= _options.BufferSize)
                    {
                        needSendData = data.Slice(index, _options.BufferSize);
                    }
                    else
                    {
                        needSendData = data.Slice(index, bytesRemaining);
                    }
                    if (_options.IsSsl)
                    {
                        await _sslStream.WriteAsync(needSendData, _lifecycleTokenSource.Token);
                    }
                    else
                    {
                        await _networkStream.WriteAsync(needSendData, _lifecycleTokenSource.Token);
                    }

                    index += needSendData.Length;
                    bytesRemaining -= needSendData.Length;
                }
            }
            catch (IOException ex)
            {
                OnDisConnected?.Invoke(this,
                    new ClientSideDisConnectEventArgs(DisConnectReason.ServerDown));
                _logger?.LogError(ex.ToString());

                throw;
            }
            catch (Exception ex)
            {
                if (ex is TaskCanceledException || ex is OperationCanceledException)
                {
                    _logger?.LogError("Send message operation was canceled.");
                }

                _logger?.LogError(ex.ToString());

                throw;
            }
            finally
            {
                _sendLock.Release();
            }
        }

        private bool IsDisconnect()
        {
            IPGlobalProperties ipProperties = IPGlobalProperties.GetIPGlobalProperties();
            TcpConnectionInformation[] tcpConnections = ipProperties.GetActiveTcpConnections()
                .Where(x => x.LocalEndPoint.Equals(_localEndPoint) && x.RemoteEndPoint.Equals(_remoteEndPoint)).ToArray();

            if (tcpConnections != null && tcpConnections.Length > 0)
            {
                TcpState stateOfConnection = tcpConnections.First().State;
                if (stateOfConnection == TcpState.Established)
                {
                    return true;
                }
            }

            return false;
        }

        /// <summary>
        /// 客户端断连
        /// </summary>
        private void DisConnect()
        {
            if (!IsConnected)
            {
                return;
            }

            _receiveDataTokenSource.Cancel();
            _socket.Close();
            IsConnected = false;
        }
    }
}
