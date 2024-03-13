using Microsoft.Extensions.Logging;
using System.Collections.Concurrent;
using System.Net;
using System.Net.Sockets;
using System.Security.Authentication;
using System.Security.Cryptography.X509Certificates;

namespace EasyTcp4Net
{
    public class EasyTcpServer
    {
        public bool IsListening { get; private set; } //是否正在监听客户端连接中

        private readonly IPAddress _ipAddress = null; //本地监听的ip地址
        private readonly ushort _port = 0; //本地监听端口
        private readonly Socket _serverSocket;  //服务端本地套接字
        private readonly EasyTcpServerOptions _options = new();//服务端总配置
        private readonly X509Certificate2 _certificate;//ssl证书对象
        private readonly SemaphoreSlim _startListenLock = new SemaphoreSlim(1, 1); //开启监听的信号量
        private readonly CancellationTokenSource _lifecycleTokenSource; //整个服务端存活的token
        private readonly CancellationTokenSource _acceptClientTokenSource;//接受客户端连接的token
        private readonly IPEndPoint _localEndPoint; //服务端本地启动的终结点
        private readonly ILogger<EasyTcpServer> _logger; //日志对象

        private readonly ConcurrentDictionary<string, ClientSession> _clients
            = new ConcurrentDictionary<string, ClientSession>();

        private Task _accetpClientsTask = null;

        /// <summary>
        /// 创建一个Tcp服务对象
        /// </summary>
        /// <param name="port">监听的端口</param>
        /// <param name="host">
        /// 监听的host
        /// 1.null or string.empty 默认监听所有的网卡地址
        /// 2.如果是域名，转换为ip地址
        /// </param>
        public EasyTcpServer(ushort port, string host = null)
        {
            if (string.IsNullOrEmpty(host) || host.Trim() == "*")
            {
                _ipAddress = IPAddress.Any;
            }
            else
            {
                _ipAddress = IPAddress.TryParse(host, out var tempAddress) ?
                    tempAddress : Dns.GetHostEntry(host).AddressList[0];
            }
            _serverSocket = new Socket(_ipAddress.AddressFamily, SocketType.Stream, ProtocolType.Tcp);
            _port = port;
            _localEndPoint = new IPEndPoint(_ipAddress, _port);
            _lifecycleTokenSource = new CancellationTokenSource();
            _acceptClientTokenSource = new CancellationTokenSource();
            IsListening = false;
        }

        /// <summary>
        /// 创建一个Tcp服务对象
        /// </summary>
        /// <param name="options">服务器对象配置</param>
        /// <exception cref="ArgumentNullException"></exception>
        public EasyTcpServer(ushort port, EasyTcpServerOptions options, string host = null) : this(port, host)
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
            _serverSocket.NoDelay = _options.NoDelay;
            _logger = options.LoggerFactory?.CreateLogger<EasyTcpServer>();
        }

        /// <summary>
        /// 开启监听
        /// </summary>
        /// <exception cref="InvalidOperationException"></exception>
        public void StartListen()
        {
            try
            {
                _startListenLock.Wait();
                if (IsListening)
                    throw new InvalidOperationException("Listener is running !");

                _serverSocket.Bind(_localEndPoint);
                if (_options.BacklogCount != null)
                {
                    _serverSocket.Listen(_options.BacklogCount.Value);
                }
                else
                {
                    _serverSocket.Listen();
                }

                IsListening = true;
                var tokenSource = CancellationTokenSource.CreateLinkedTokenSource(_acceptClientTokenSource.Token,
                    _lifecycleTokenSource.Token);

                //开始接受客户端请求
                _accetpClientsTask = Task.Factory
                    .StartNew(AcceptClientAsync, tokenSource.Token, TaskCreationOptions.LongRunning, TaskScheduler.Current);
                //开启客户端空闲检查
                //TODO
            }
            catch (Exception ex)
            {
                if (ex is TaskCanceledException || ex is OperationCanceledException)
                {
                    _logger?.LogInformation("Listener was canceled.");
                }
                else
                {
                    throw;
                }
            }
            finally
            {
                _startListenLock.Release();
            }
        }

        private async Task AcceptClientAsync()
        {
            while (!_acceptClientTokenSource.Token.IsCancellationRequested)
            {
                if (!IsListening)
                    throw new InvalidOperationException(nameof(AcceptClientAsync) + ":listening status error");

                ClientSession clientSession = null;
                try
                {
                    if (_options.ConnectionsLimit != null && _clients.Count >= _options.ConnectionsLimit)
                    {
                        await Task.Delay(200).ConfigureAwait(false);
                        continue;
                    }

                    var newClientSocket = await _serverSocket.AcceptAsync(_acceptClientTokenSource.Token);
                    clientSession = new ClientSession(newClientSocket);
                    if (_options.IsSsl)
                    {
                        CancellationTokenSource _sslTimeoutTokenSource = new CancellationTokenSource();
                        _sslTimeoutTokenSource.CancelAfter(TimeSpan.FromSeconds(3));
                        CancellationTokenSource sslTokenSource = CancellationTokenSource
                            .CreateLinkedTokenSource(_acceptClientTokenSource.Token, _sslTimeoutTokenSource.Token);

                        var result = await clientSession
                            .SslAuthenticateAsync(_certificate, _options.AllowingUntrustedSSLCertificate,
                                _options.MutuallyAuthenticate, _options.CheckCertificateRevocation, sslTokenSource.Token);

                        if (!result)
                        {
                            clientSession.Dispose();
                            continue;
                        }
                    }

                    _clients.TryAdd(clientSession.RemoteEndPoint.ToString(), clientSession);
                    Task.Factory.StartNew(() =>
                    {

                    });
                    _logger?.LogInformation($"{clientSession.RemoteEndPoint}：connected.");
                }
                catch (Exception ex)
                {
                    _logger?.LogError(ex.ToString());
                    if (ex is TaskCanceledException || ex is OperationCanceledException || ex is AuthenticationException)
                    {
                        clientSession?.Dispose();
                    }
                }
            }
        }

        private async Task ReceiveClientDataAsync(ClientSession clientSession)
        {
            while (!clientSession._lifecycleTokenSource.Token.IsCancellationRequested)
            {
                if (clientSession.IsDisposed)
                    break;

                if (!clientSession.IsConnected())
                {
                    break;
                }

                await clientSession.ReceiveDataAsync(_options.BufferSize);
            }
        }
    }
}
