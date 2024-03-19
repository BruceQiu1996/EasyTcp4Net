namespace EasyTcp4Net.ServerTest
{
    internal class Program
    {
        static void Main(string[] args)
        {
            Console.WriteLine("Hello World,this is Server");
            EasyTcpServer easyTcpServer = new EasyTcpServer(7001, new EasyTcpServerOptions()
            {
                IdleSessionsCheck = false,
                KeepAlive = true,
                CheckSessionsIdleMs = 10 * 1000
            });
            easyTcpServer
                .SetReceiveFilter(new FixedCharPackageFilter('\n'));
            easyTcpServer.StartListen();

            int index = 0;
            int bytes = 0;
            easyTcpServer.OnReceivedData += async (obj, data) =>
            {
                index++;
                bytes += data.Data.Length;
                Console.WriteLine($"收到消息：{index}");
                Console.WriteLine($"收到长度：{bytes}");
            };
           
            Console.ReadLine();
        }
    }
}
