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
        public event EventHandler<ServerDataReceiveEventArgs> OnReceivedData;
        public event EventHandler<ServerSideClientConnectionChangeEventArgs> OnClientConnectionChanged;
        private readonly IPAddress _ipAddress = null; //本地监听的ip地址
        private readonly ushort _port = 0; //本地监听端口
        private Socket _serverSocket;  //服务端本地套接字
        private readonly EasyTcpServerOptions _options = new();//服务端总配置
        private readonly X509Certificate2 _certificate;//ssl证书对象
        private readonly SemaphoreSlim _startListenLock = new SemaphoreSlim(1, 1); //开启监听的信号量
        private CancellationTokenSource _lifecycleTokenSource; //整个服务端存活的token
        private CancellationTokenSource _acceptClientTokenSource;//接受客户端连接的token
        private readonly IPEndPoint _localEndPoint; //服务端本地启动的终结点
        private readonly ILogger<EasyTcpServer> _logger; //日志对象

        private readonly ConcurrentDictionary<string, ClientSession> _clients = new ConcurrentDictionary<string, ClientSession>();

        private Task _accetpClientsTask = null;
        private Task _checkIdleSessionsTask = null;
        private IPackageFilter _receivePackageFilter = null; //接收数据包的拦截处理器
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
            if (port < 0 || port > 65535)
                throw new InvalidDataException("Unexcepted port number!");

            if (string.IsNullOrEmpty(host) || host.Trim() == "*")
            {
                _ipAddress = IPAddress.Any;
            }
            else
            {
                _ipAddress = IPAddress.TryParse(host, out var tempAddress) ?
                    tempAddress : Dns.GetHostEntry(host).AddressList[0];
            }

            _port = port;
            _localEndPoint = new IPEndPoint(_ipAddress, _port);
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

                StartSocketListen();
                var tokenSource = CancellationTokenSource.CreateLinkedTokenSource(_acceptClientTokenSource.Token,
                    _lifecycleTokenSource.Token);

                //开始接受客户端请求
                _accetpClientsTask = Task.Factory
                    .StartNew(AcceptClientAsync, tokenSource.Token, TaskCreationOptions.LongRunning, TaskScheduler.Current);
                //开启客户端空闲检查
                if (_options.IdleSessionsCheck)
                {
                    _checkIdleSessionsTask = Task.Factory
                        .StartNew(CheckIdleConnectionsAsync, tokenSource.Token, TaskCreationOptions.LongRunning, TaskScheduler.Current);
                }
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

        private void StartSocketListen()
        {
            _serverSocket = new Socket(_ipAddress.AddressFamily, SocketType.Stream, ProtocolType.Tcp);
            if (_options.KeepAlive)
                _serverSocket.SetKeepAlive(_options.KeepAliveIntvl, _options.KeepAliveTime, _options.KeepAliveProbes);
            _serverSocket.NoDelay = _options.NoDelay;

            _lifecycleTokenSource = new CancellationTokenSource();
            _acceptClientTokenSource = new CancellationTokenSource();
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
        }

        /// <summary>
        /// 添加接收数据的过滤处理器
        /// </summary>
        /// <param name="filters"></param>
        public void SetReceiveFilter(IPackageFilter filter)
        {
            if (filter == null)
                return;

            _receivePackageFilter = filter;
        }

        /// <summary>
        /// 开启接收客户端的线程
        /// </summary>
        /// <returns></returns>
        /// <exception cref="InvalidOperationException">已经取消或者没有启动监听后产生的异常</exception>
        private async Task AcceptClientAsync()
        {
            if (!IsListening)
                throw new InvalidOperationException(nameof(AcceptClientAsync) + ":listening status error");

            while (!_acceptClientTokenSource.Token.IsCancellationRequested)
            {
                ClientSession clientSession = null;
                try
                {
                    if (_options.ConnectionsLimit != null && _clients.Count >= _options.ConnectionsLimit)
                    {
                        await Task.Delay(200).ConfigureAwait(false);
                        continue;
                    }

                    if (!IsListening)
                    {
                        StartSocketListen();
                    }

                    var newClientSocket = await _serverSocket.AcceptAsync(_acceptClientTokenSource.Token);
                    clientSession = new ClientSession(newClientSocket, _options.BufferSize, _options.MaxPipeBufferSize, _receivePackageFilter, OnReceivedData);
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
                            await clientSession.DisposeAsync();
                            continue;
                        }
                    }

                    if (_options.KeepAlive) newClientSocket.SetKeepAlive(_options.KeepAliveIntvl, _options.KeepAliveTime, _options.KeepAliveProbes);

                    _clients.TryAdd(clientSession.RemoteEndPoint.ToString(), clientSession);
                    clientSession.Connected = true;
                    OnClientConnectionChanged?.Invoke(this, new ServerSideClientConnectionChangeEventArgs(clientSession, ConnectsionStatus.Connected));
                    var _ = Task.Factory.StartNew(async () =>
                    {
                        await ReceiveClientDataAsync(clientSession);
                    });
                    _logger?.LogInformation($"{clientSession.RemoteEndPoint}：connected.");

                    if (_clients.Count >= _options.ConnectionsLimit)
                    {
                        _logger?.LogInformation($"The maximum number of connections has been exceeded");
                        _serverSocket.Close();
                        _serverSocket.Dispose();
                        IsListening = false;
                    }
                }
                catch (Exception ex)
                {
                    _logger?.LogError(ex.ToString());
                    if (ex is TaskCanceledException || ex is OperationCanceledException || ex is AuthenticationException)
                    {
                        if (clientSession != null)
                        {
                            await clientSession.DisposeAsync();
                        }
                    }
                }
            }
        }

        /// <summary>
        /// 接收客户端信息
        /// </summary>
        /// <param name="clientSession">客户端会话对象</param>
        /// <returns></returns>
        private async Task ReceiveClientDataAsync(ClientSession clientSession)
        {
            try
            {
                await clientSession.ReceiveDataAsync();
            }
            catch (SocketException ex)
            {
                _logger?.LogError($"Socket reeceive data error:{ex}");
            }
            catch (TaskCanceledException ex)
            {
                _logger?.LogError($"Receive data task is canceled:{ex}");
            }
            catch (OperationCanceledException ex)
            {
                _logger?.LogError($"Receive data task is canceled:{ex}");
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex.ToString());
            }

            _clients.TryRemove(clientSession.RemoteEndPoint.ToString(), out var _);
            OnClientConnectionChanged?.Invoke(this, new ServerSideClientConnectionChangeEventArgs(clientSession, ConnectsionStatus.DisConnected));
            await clientSession.DisposeAsync();
        }

        /// <summary>
        /// 检查空闲的客户端连接
        /// </summary>
        /// <returns></returns>
        private async Task CheckIdleConnectionsAsync()
        {
            while (!_lifecycleTokenSource.Token.IsCancellationRequested)
            {
                var expireTime = DateTime.UtcNow.AddMilliseconds(-1 * _options.CheckSessionsIdleMs);
                foreach (var clientEntry in _clients)
                {
                    if (clientEntry.Value.LastActiveTime <= expireTime)
                    {
                        // 关闭空闲连接
                        await DisconnectClientAsync(clientEntry.Value);
                    }
                }

                await Task.Delay(1000).ConfigureAwait(false);
            }
        }

        public async Task DisconnectClientAsync(ClientSession clientSession)
        {
            if (!_clients.TryGetValue(clientSession.RemoteEndPoint.ToString(), out var temp))
            {
                _logger?.LogInformation("ClientSession does not exist when wanna DisconnectClient");
            }

            if (clientSession != null)
            {
                if (!clientSession._lifecycleTokenSource.IsCancellationRequested)
                {
                    clientSession._lifecycleTokenSource.Cancel();
                }

                await clientSession.DisposeAsync();
            }
        }

        /// <summary>
        /// Stop accepting new connections.
        /// </summary>
        public async Task CloseAsync()
        {
            if (!IsListening)
                return;

            IsListening = false;
            _serverSocket.Close();
            _serverSocket.Dispose();
            _acceptClientTokenSource?.Cancel();
            _lifecycleTokenSource?.Cancel();

            foreach (var clients in _clients)
            {
                await clients.Value.DisposeAsync();
            }

            _clients.Clear();
        }

        #region send data
        public async Task SendAsync(string sessionId, byte[] data)
        {
            var sessions =
                _clients.Where(x => x.Value.SessionId == sessionId);

            await Parallel.ForEachAsync(sessions, _lifecycleTokenSource.Token, async (item, token) =>
            {
                if (!token.IsCancellationRequested)
                {
                    await item.Value.SendAsync(data);
                }
            }).ConfigureAwait(false);
        }

        public async Task SendAsync(IPEndPoint endpoint, byte[] data)
        {
            var result =
                _clients.TryGetValue(endpoint.ToString(), out var client);

            if (result)
            {
                await client.SendAsync(data);
            }
        }

        public async Task SendAsync(string sessionId, Memory<byte> data)
        {
            await SendAsync(sessionId, data.ToArray());
        }

        public async Task SendAsync(ClientSession session, byte[] data)
        {
            if (_clients.Where(x => x.Value == session).Count() > 0)
            {
                await session.SendAsync(data);
            }
        }

        public async Task SendAsync(ClientSession session, Memory<byte> data)
        {
            if (_clients.Where(x => x.Value == session).Count() > 0)
            {
                await session.SendAsync(data);
            }
        }
        #endregion
    }
}
