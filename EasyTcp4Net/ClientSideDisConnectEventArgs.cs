using System.ComponentModel;

namespace EasyTcp4Net
{
    /// <summary>
    /// 客户端感知断线事件
    /// </summary>
    public class ClientSideDisConnectEventArgs
    {
        public DisConnectReason Reason { get; private set; }
        public ClientSideDisConnectEventArgs(DisConnectReason disConnectReason)
        {
            Reason = disConnectReason;
        }
    }

    public enum DisConnectReason 
    {
        [Description("主动断开")]
        Normol,
        [Description("服务端断开")]
        ServerDown,
        [Description("未知")]
        UnKnown
    }
}
