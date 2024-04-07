using EasyTcp4Net;

namespace FileSenderServer
{
    /// <summary>
    /// 局域网文件发送服务端
    /// </summary>
    internal class Program
    {
        static void Main(string[] args)
        {
            EasyTcpServer easyTcpServer = new EasyTcpServer(7007);
            easyTcpServer.SetReceiveFilter(new FixedHeaderPackageFilter(6, 2, 4));
            easyTcpServer.StartListen();

            easyTcpServer.OnClientConnectionChanged += (obj, e) =>
            {

            };

            easyTcpServer.OnReceivedData += (obj, e) =>
            {

            };

            Console.Read();
        }
    }
}
