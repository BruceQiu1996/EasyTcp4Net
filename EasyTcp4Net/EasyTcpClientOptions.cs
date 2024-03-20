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
        private int _connectTimeout = 30 * 1000;
        public int ConnectTimeout
        {
            get => _connectTimeout;
            set
            {
                if (value <= 0)
                {
                    throw new ArgumentException("ConnectTimeout must be greater then zero");
                }

                _connectTimeout = value;
            }
        }

        /// <summary>
        /// 连接失败尝试次数
        /// 默认值：0(不重试)
        /// </summary>
        private int _connectRetryTimes = 0;
        public int ConnectRetryTimes
        {
            get => _connectRetryTimes;
            set
            {
                if (value < 0)
                {
                    throw new ArgumentException("ConnectRetryTimes must be greater or equal then zero");
                }

                _connectRetryTimes = value;
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
                if (value <= 0)
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
                if (value <= 0)
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

        /// <summary>
        /// 是否启动操作系统的tcp keepalive机制
        /// 不同操作系统实现keepalive机制并不相同
        /// </summary>
        public bool KeepAlive { get; set; } = false;

        private int _keepAliveTime = 3600;
        /// <summary>
        ///  KeepAlive的空闲时长，或者说每次正常发送心跳的周期，默认值为3600s（1小时）
        /// </summary>
        public int KeepAliveTime
        {
            get => _keepAliveTime;
            set
            {
                if (value <= 0)
                {
                    throw new ArgumentException("KeepAliveTime must be greater then zero");
                }

                _keepAliveTime = value;
            }
        }

        private int _keepAliveProbes = 9;
        /// <summary>
        /// KeepAlive之后设置最大允许发送保活探测包的次数，到达此次数后直接放弃尝试，并关闭连接
        /// 默认值：9次
        /// </summary>
        public int KeepAliveProbes
        {
            get => _keepAliveProbes;
            set
            {
                if (value <= 0)
                {
                    throw new ArgumentException("KeepAliveProbes must be greater then zero");
                }

                _keepAliveProbes = value;
            }
        }

        private int _keepAliveIntvl = 60;
        /// <summary>
        /// 没有接收到对方确认，继续发送KeepAlive的发送频率，默认值为60s
        /// 单位：秒
        /// 默认值 60（1分钟）
        /// </summary>
        public int KeepAliveIntvl
        {
            get => _keepAliveIntvl;
            set
            {
                if (value <= 0)
                {
                    throw new ArgumentException("KeepAliveIntvl must be greater then zero");
                }

                _keepAliveIntvl = value;
            }
        }

        private int _maxPipeBufferSize = 1024 * 1024 * 4;
        /// <summary>
        /// 待处理数据队列最大缓存,如果有粘包断包的过滤器，要大于单个包的大小，防止卡死
        /// 用于流量控制，背压
        /// 单位：字节
        /// 默认值 4MB
        /// </summary>
        public int MaxPipeBufferSize
        {
            get => _maxPipeBufferSize;
            set
            {
                if (value <= 0)
                {
                    throw new ArgumentException("MaxPipeBufferSize must be greater then zero");
                }

                _maxPipeBufferSize = value;
            }
        }
    }
}
