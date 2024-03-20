namespace EasyTcp4Net.ServerTest
{
    internal class Program
    {
        static void Main(string[] args)
        {
            Console.WriteLine("Hello World,this is Server");
            EasyTcpServer easyTcpServer = new EasyTcpServer(7001, new EasyTcpServerOptions()
            {
                IsSsl = true,
                PfxCertFilename = "test.pfx",
                PfxPassword = "123456",
                IdleSessionsCheck = false,
                KeepAlive = true,
                CheckSessionsIdleMs = 10 * 1000
            });
            //参数分别为：数据包头长度，数据包体长度，数据包体长度字节数，是否小端在前
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
