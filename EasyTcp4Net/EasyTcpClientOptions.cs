using Microsoft.Extensions.Logging;

namespace EasyTcp4Net
{
    /// <summary>
    /// tcp server配置类
    /// </summary>
    public class EasyTcpClientOptions
    {
        public EasyTcpClientOptions() { }

        /// <summary>
        /// 是否不使用 Nagle's算法
        /// 是为了提高实际带宽利用率设计的算法，其做法是合并小的TCP 包为一个
        /// 避免了过多的小报文的 过大TCP头所浪费的带宽
        /// </summary>
        public bool NoDelay { get; set; } = true;
        /// <summary>
        /// 流数据缓冲区大小
        /// 单位：字节
        /// 默认值：2kb
        /// </summary>
        private int _bufferSize = 2 * 1024;
        public int BufferSize
        {
            get => _bufferSize;
            set
            {
                if (value <= 0)
                {
                    throw new ArgumentException("BufferSize must be greater then zero");
                }

                _bufferSize = value;
            }
        }

        /// <summary>
        /// 连接超时时间
        /// 单位：毫秒
        /// 默认值：30秒
        /// </summary>
        private int _connectionTimeout = 30 * 1000;
        public int ConnectionTimeout
        {
            get => _connectionTimeout;
            set
            {
                if (value <= 0)
                {
                    throw new ArgumentException("ConnectionTimeout must be greater then zero");
                }

                _connectionTimeout = value;
            }
        }

        /// <summary>
        /// 连接等待/挂起连接数量
        /// 默认值：10秒
        /// 单位：毫秒
        /// </summary>
        private int _readTimeout = 10 * 1000;
        public int ReadTimeout
        {
            get => _readTimeout;
            set
            {
                if (_readTimeout <=0)
                {
                    throw new ArgumentException("ReadTimeout must be greater then zero");
                }

                _readTimeout = value;
            }
        }

        /// <summary>
        /// 连接等待/挂起连接数量
        /// 默认值：10秒
        /// 单位：毫秒
        /// </summary>
        private int _writeTimeout = 10 * 1000;
        public int WriteTimeout
        {
            get => _writeTimeout;
            set
            {
                if (_writeTimeout <= 0)
                {
                    throw new ArgumentException("WriteTimeout must be greater then zero");
                }

                _writeTimeout = value;
            }
        }

        /// <summary>
        /// 是否使用ssl连接
        /// 默认值：false
        /// </summary>
        public bool IsSsl { get; set; } = false;
        /// <summary>
        /// ssl证书
        /// </summary>
        public string PfxCertFilename { get; set; }
        /// <summary>
        /// ssl证书密钥
        /// </summary>
        public string PfxPassword { get; set; }
        /// <summary>
        /// 是否允许不受信任的ssl证书
        /// 默认值：true
        /// </summary>
        public bool AllowingUntrustedSSLCertificate { get; set; } = true;
        /// <summary>
        /// 是否双向的ssl验证,标识了客户端是否需要提供证书
        /// 默认值：false
        /// </summary>
        public bool MutuallyAuthenticate { get; set; } = false;
        /// <summary>
        /// 是否检查整数的吊销列表
        /// 默认值：true
        /// </summary>
        public bool CheckCertificateRevocation { get; set; } = true;
        /// <summary>
        /// 日志工厂
        /// </summary>
        public ILoggerFactory LoggerFactory { get; set; }
    }
}
