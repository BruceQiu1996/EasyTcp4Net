using CommunityToolkit.HighPerformance.Buffers;
using System.Buffers;
using System.IO.Pipelines;
using System.Net;
using System.Net.NetworkInformation;
using System.Net.Security;
using System.Net.Sockets;
using System.Runtime.InteropServices;
using System.Security.Cryptography.X509Certificates;

namespace EasyTcp4Net
{
    /// <summary>
    /// 客户端会话对象
    /// </summary>
    public class ClientSession : IDisposable
    {
        /// <summary>
        /// 会话id
        /// 添加了身份认证后，可以讲该值绑定账号身份唯一标识
        /// 可以知道session属于哪个用户
        /// 多端登录可以知道哪些session属于同一用户
        /// </summary>
        public string SessionId { get; set; } = Guid.NewGuid().ToString();
        /// <summary>
        /// 对于服务端来说，远程终结点就是客户端的本地套接字终结点
        /// </summary>
        public IPEndPoint RemoteEndPoint => _socket == null ? null : _socket.RemoteEndPoint as IPEndPoint;
        /// <summary>
        /// 对于服务端来说，本地终结点就是客户端在远端对应的套接字终结点
        /// </summary>
        public IPEndPoint LocalEndPoint => _socket == null ? null : _socket.LocalEndPoint as IPEndPoint;
        public bool IsSslAuthenticated { get; internal set; } = false;
        public DateTime? SslAuthenticatedTime { get; internal set; } = null;
        public DateTime LastActiveTime { get; set; } = DateTime.UtcNow;
        internal NetworkStream NetworkStream { get; private set; }
        internal SslStream SslStream { get; private set; }
        public bool IsDisposed { get; private set; }
        public bool Connected { get; internal set; }
        public PipeReader PipeReader => _pipe.Reader;
        public PipeWriter PipeWriter => _pipe.Writer;

        private Task _processDataTask;
        private Socket _socket;
        private Pipe _pipe;
        private int _bufferSize;
        private readonly SemaphoreSlim _sendLock = new SemaphoreSlim(1, 1); //发送数据的信号量
        internal readonly CancellationTokenSource _lifecycleTokenSource;
        public ClientSession(Socket socket, int bufferSize)
        {
            _socket = socket;
            _pipe = new Pipe();
            _bufferSize = bufferSize;
            NetworkStream = new NetworkStream(socket);
            _lifecycleTokenSource = new CancellationTokenSource();
            _processDataTask = Task.Factory.StartNew(ReadPipeAsync, _lifecycleTokenSource.Token, TaskCreationOptions.LongRunning, TaskScheduler.Current);
        }

        /// <summary>
        /// 客户端会话的ssl认证
        /// </summary>
        /// <param name="x509Certificate">ssl证书</param>
        /// <param name="allowingUntrustedSSLCertificate">是否允许不受信任的证书</param>
        /// <param name="mutuallyAuthenticate">双向认证，即客户端是否需要提供证书</param>
        /// <param name="checkCertificateRevocation">是否检查证书列表的吊销列表</param>
        /// <param name="cancellationToken">任务令牌</param>
        /// <returns></returns>
        internal async Task<bool> SslAuthenticateAsync(X509Certificate2 x509Certificate,
            bool allowingUntrustedSSLCertificate,
            bool mutuallyAuthenticate,
            bool checkCertificateRevocation,
            CancellationToken cancellationToken)
        {

            if (allowingUntrustedSSLCertificate)
            {
                SslStream = new SslStream(NetworkStream, false,
                    (obj, certificate, chain, error) => true);
            }
            else
            {
                SslStream = new SslStream(NetworkStream, false);
            }

            try
            {
                //serverCertificate：用于对服务器进行身份验证的 X509Certificate
                //clientCertificateRequired：一个 Boolean 值，指定客户端是否必须为身份验证提供证书
                //checkCertificateRevocation：一个 Boolean 值，指定在身份验证过程中是否检查证书吊销列表
                await SslStream.AuthenticateAsServerAsync(new SslServerAuthenticationOptions()
                {
                    ServerCertificate = x509Certificate,
                    ClientCertificateRequired = mutuallyAuthenticate,
                    CertificateRevocationCheckMode = checkCertificateRevocation ? X509RevocationMode.Online : X509RevocationMode.NoCheck
                }, cancellationToken).ConfigureAwait(false);

                if (!SslStream.IsEncrypted || !SslStream.IsAuthenticated)
                {
                    return false;
                }

                if (mutuallyAuthenticate && !SslStream.IsMutuallyAuthenticated)
                {
                    return false;
                }
            }
            catch (Exception)
            {
                throw;
            }

            IsSslAuthenticated = true;
            SslAuthenticatedTime = DateTime.UtcNow;

            return true;
        }

        /// <summary>
        /// 读取数据
        /// </summary>
        /// <param name="bufferSize">缓冲区大小</param>
        /// <returns></returns>
        /// <exception cref="SocketException">读取到长度为0的数据，默认为断开了</exception>
        internal async Task ReceiveDataAsync()
        {
            while (!_lifecycleTokenSource.Token.IsCancellationRequested)
            {
                if (IsDisposed)
                    break;

                try
                {
                    if (!IsConnected())
                    {
                        break;
                    }

                    Memory<byte> buffer = PipeWriter.GetMemory(_bufferSize);
                    int readCount;
                    if (IsSslAuthenticated)
                    {
                        readCount = await SslStream.ReadAsync(buffer, _lifecycleTokenSource.Token)
                            .ConfigureAwait(false);
                    }
                    else
                    {
                        readCount = await NetworkStream.ReadAsync(buffer, _lifecycleTokenSource.Token)
                            .ConfigureAwait(false);
                    }

                    if (readCount > 0)
                    {
                        LastActiveTime = DateTime.UtcNow;
                        PipeWriter.Advance(readCount);
                    }
                    else
                    {
                        throw new SocketException();
                    }

                    FlushResult result = await PipeWriter.FlushAsync().ConfigureAwait(false);
                    if (result.IsCompleted)
                    {
                        break;
                    }
                }
                catch (Exception) 
                {
                    throw;
                }
            }

            PipeWriter.Complete();
        }

        internal async Task ReadPipeAsync()
        {
            int index = 0;
            FixedHeaderPackageFilter fixedHeaderPackageFilter = new FixedHeaderPackageFilter(7,3,4);
            while (!_lifecycleTokenSource.Token.IsCancellationRequested)
            {
                ReadResult result = await PipeReader.ReadAsync();
                ReadOnlySequence<byte> buffer = result.Buffer;
                ReadOnlySequence<byte> data;
                do
                {
                    data = fixedHeaderPackageFilter.ResolvePackage(ref buffer);
                    if (!data.IsEmpty) 
                    {
                        Console.WriteLine(++index);
                    }
                }
                while (!data.IsEmpty);
                PipeReader.AdvanceTo(buffer.Start, buffer.End);
            }

            PipeReader.Complete();
        }

        public async Task SendAsync(byte[] data)
        {
            if (data == null || data.Length < 1)
                throw new ArgumentNullException(nameof(data));

            await SendInternalAsync(data);
        }

        public async Task SendAsync(Memory<byte> data)
        {
            if (data.IsEmpty || data.Length < 1)
                throw new ArgumentNullException(nameof(data));

            await SendInternalAsync(data);
        }

        private async Task SendInternalAsync(Memory<byte> data)
        {
            if (!Connected)
                return;

            LastActiveTime = DateTime.UtcNow;
            int bytesRemaining = data.Length;
            int index = 0;

            try
            {
                _sendLock.Wait();
                while (bytesRemaining > 0)
                {
                    Memory<byte> needSendData = null;
                    if (bytesRemaining >= _bufferSize)
                    {
                        needSendData = data.Slice(index, _bufferSize);
                    }
                    else
                    {
                        needSendData = data.Slice(index, bytesRemaining);
                    }
                    if (IsSslAuthenticated)
                    {
                        await SslStream.WriteAsync(needSendData, _lifecycleTokenSource.Token);
                    }
                    else
                    {
                        await NetworkStream.WriteAsync(needSendData, _lifecycleTokenSource.Token);
                    }

                    index += needSendData.Length;
                    bytesRemaining -= needSendData.Length;
                }
            }
            finally
            {
                _sendLock.Release();
            }
        }

        /// <summary>
        /// 判断客户端的会话是否连接着
        /// </summary>
        /// <returns>是否连接，true表示已连接，false表示未连接</returns>
        internal bool IsConnected()
        {
            return _socket.CheckConnect();
        }

        public void Dispose()
        {
            if (!IsDisposed)
            {
                InternalDispose();
            }
        }

        private void InternalDispose()
        {
            _lifecycleTokenSource?.Cancel();
            _socket?.Dispose();
            NetworkStream?.Close();
            NetworkStream?.Dispose();
            SslStream?.Close();
            SslStream?.Dispose();
            IsDisposed = true;
        }
    }
}
