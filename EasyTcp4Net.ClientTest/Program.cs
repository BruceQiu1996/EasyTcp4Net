namespace EasyTcp4Net.ClientTest
{
    internal class Program
    {
        async static Task Main(string[] args)
        {
            await Task.Delay(1000);

            EasyTcpClient easyTcpClient = new EasyTcpClient("127.0.0.1", 7001);
            await easyTcpClient.ConnectAsync();
            await easyTcpClient.SendAsync(new byte[] { 1, 2, 3, 4, 5, 6, 7, 8, 9, 10, 11 });

            Console.Read();
            Console.WriteLine("Hello, World!");
        }
    }
}
