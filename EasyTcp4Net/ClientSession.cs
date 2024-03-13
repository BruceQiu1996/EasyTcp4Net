using System.Net;
using System.Net.Security;
using System.Net.Sockets;
using System.Security.Cryptography.X509Certificates;

namespace EasyTcp4Net
{
    /// <summary>
    /// 客户端会话对象
    /// </summary>
    public class ClientSession : IDisposable
    {
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
        public NetworkStream NetworkStream { get; private set; }
        public SslStream SslStream { get; private set; }
        public bool IsDisposed { get; private set; }

        private Socket _socket;
        internal readonly CancellationTokenSource _lifecycleTokenSource;
        public ClientSession(Socket socket)
        {
            _socket = socket;
            NetworkStream = new NetworkStream(socket);
            _lifecycleTokenSource = new CancellationTokenSource();
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

        internal async Task<Memory<byte>> ReceiveDataAsync() 
        {
            
        }

        public void Dispose()
        {
            throw new NotImplementedException();
        }
    }
}
