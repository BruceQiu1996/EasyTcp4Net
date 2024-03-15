using System.Net.NetworkInformation;
using System.Net.Sockets;

namespace EasyTcp4Net
{
    internal static class SocketExtension
    {
        /// <summary>
        /// 开启Socket的KeepAlive
        /// 设置tcp协议的一些KeepAlive参数
        /// </summary>
        /// <param name="socket"></param>
        /// <param name="tcpKeepAliveInterval"></param>
        /// <param name="tcpKeepAliveTime"></param>
        /// <param name="tcpKeepAliveRetryCount"></param>
        internal static void SetKeepAlive(this Socket socket, int tcpKeepAliveInterval, int tcpKeepAliveTime, int tcpKeepAliveRetryCount)
        {
            socket.SetSocketOption(SocketOptionLevel.Socket, SocketOptionName.KeepAlive, true);
            socket.SetSocketOption(SocketOptionLevel.Tcp, SocketOptionName.TcpKeepAliveInterval, tcpKeepAliveInterval);
            socket.SetSocketOption(SocketOptionLevel.Tcp, SocketOptionName.TcpKeepAliveTime, tcpKeepAliveTime);
            socket.SetSocketOption(SocketOptionLevel.Tcp, SocketOptionName.TcpKeepAliveRetryCount, tcpKeepAliveRetryCount);
        }

        /// <summary>
        /// 检查套接字是否连接着
        /// </summary>
        /// <param name="socket"></param>
        /// <returns></returns>
        internal static bool CheckConnect(this Socket socket)
        {
            if (socket == null) return false;

            try
            {
                var state = IPGlobalProperties.GetIPGlobalProperties()
                    .GetActiveTcpConnections()
                        .FirstOrDefault(x =>
                            x.LocalEndPoint.Equals(socket.LocalEndPoint)
                            && x.RemoteEndPoint.Equals(socket.RemoteEndPoint));

                if (state == default(TcpConnectionInformation)
                    || state.State == TcpState.Unknown
                    || state.State == TcpState.FinWait1 //向服务端发起断开请求，进入fin1
                    || state.State == TcpState.FinWait2 //收到服务器Ack,等待服务器，进入fin2
                    || state.State == TcpState.Closed
                    || state.State == TcpState.Closing
                    || state.State == TcpState.CloseWait)
                {
                    return false;
                }

                return !((socket.Poll(0, SelectMode.SelectRead) && (socket.Available == 0)) || !socket.Connected);
            }
            catch (SocketException)
            {
                return false;
            }
        }
    }
}
