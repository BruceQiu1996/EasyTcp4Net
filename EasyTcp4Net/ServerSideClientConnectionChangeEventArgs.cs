using System.ComponentModel;

namespace EasyTcp4Net
{
    /// <summary>
    /// 服务端某个连接断开的事件
    /// </summary>
    public class ServerSideClientConnectionChangeEventArgs
    {
        public ClientSession ClientSession { get; set; }
        public ConnectsionStatus Status { get; private set; }
        /// <summary>
        /// 事件构造器
        /// </summary>
        /// <param name="clientSession">客户端连接对象</param>
        /// <param name="connectsionStatus">连接状态</param>
        public ServerSideClientConnectionChangeEventArgs(ClientSession clientSession, ConnectsionStatus connectsionStatus)
        {
            ClientSession = clientSession;
            Status = connectsionStatus;
        }
    }

    public enum ConnectsionStatus
    {
        [Description("连接")]
        Connected,
        [Description("断开")]
        DisConnected
    }
}
