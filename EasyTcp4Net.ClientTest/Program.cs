namespace EasyTcp4Net.ClientTest
{
    internal class Program
    {
        async static Task Main(string[] args)
        {

            Console.WriteLine("Hello World,this is Client");

            EasyTcpClient easyTcpClient = new EasyTcpClient("127.0.0.1", 7001, new EasyTcpClientOptions() 
            {
                KeepAlive = true,
            });
            await Task.Delay(1500);
            await easyTcpClient.ConnectAsync();

            foreach (var index in Enumerable.Range(0, 200))
            {
                await easyTcpClient.SendAsync(new byte[] { 1, 2, 3});
            }

            easyTcpClient.OnReceivedData += (obj, e) =>
            {
                Console.WriteLine(string.Join(',', e.Data));
                Console.WriteLine("\n");
            };

            Console.Read();
        }
    }
}
