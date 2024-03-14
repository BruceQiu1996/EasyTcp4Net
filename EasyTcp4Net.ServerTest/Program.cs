namespace EasyTcp4Net.ServerTest
{
    internal class Program
    {
        static void Main(string[] args)
        {
            EasyTcpServer easyTcpServer = new EasyTcpServer(7001);
            easyTcpServer.StartListen();

            easyTcpServer.OnReceivedData += (obj, data) =>
            {

            };
            Console.ReadLine();
        }
    }
}
