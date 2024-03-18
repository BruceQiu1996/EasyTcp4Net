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

            var head = new byte[] { 2, 3, 3, 0, 20, 0, 0 };
            var data = new byte[5127];
            foreach (int index in Enumerable.Range(0, head.Length)) 
            {
                data[index] = head[index];
            }


            Random random = new Random();
            foreach (var index in Enumerable.Range(0, 200))
            {
                foreach (var inindex in Enumerable.Range(0, 5120)) 
                {
                    data[inindex + 7] = (byte)random.Next(0, 120);
                }

                await easyTcpClient.SendAsync(data);
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
