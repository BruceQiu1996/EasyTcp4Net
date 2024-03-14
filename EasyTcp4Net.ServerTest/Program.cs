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
                CheckSessionsIdleMs = 10 * 1000
            });
            easyTcpServer.StartListen();

            easyTcpServer.OnReceivedData += async (obj, data) =>
            {
                Console.WriteLine(string.Join(',', data.Data));
                  Console.WriteLine("\n");
                await data.Session.SendAsync(new byte[] { 12, 13, 14, 15 });
            };

            Console.ReadLine();
        }
    }
}
