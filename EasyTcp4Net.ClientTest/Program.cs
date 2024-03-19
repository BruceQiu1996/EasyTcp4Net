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
            await Task.Delay(500);
            await easyTcpClient.ConnectAsync();
            var a = BitConverter.GetBytes(65536 * 20);
            var head = new byte[] { 2, 3, 3, 0, 20, 0, 0 };
            var data = new byte[5128];
            foreach (int index in Enumerable.Range(0, head.Length)) 
            {
                data[index] = head[index];
            }


            Random random = new Random();
            int count = 0;
            foreach (var index in Enumerable.Range(0, 200))
            {

                foreach (var inindex in Enumerable.Range(0, 5120))
                {
                    data[inindex + 7] = (byte)random.Next(0, 120);
                }
                data[5127] = (byte)'\n';
                Memory<byte> sendm = new Memory<byte>(data);
                var split = random.Next(100, 1000);
                await easyTcpClient.SendAsync(sendm.Slice(0, split));
                await Task.Delay(10);
                await easyTcpClient.SendAsync(sendm.Slice(split));

                Console.WriteLine(++count);
            }

            

            Memory<byte> bytes = new Memory<byte>(data);
            //await easyTcpClient.SendAsync(bytes.Slice(0,200));

            //await Task.Delay(2000);
            // await easyTcpClient.SendAsync(bytes.Slice(200));
            ////foreach (var index in Enumerable.Range(0, 200))
            ////{
                

               
            ////}

            //easyTcpClient.OnReceivedData += (obj, e) =>
            //{
            //    Console.WriteLine(string.Join(',', e.Data));
            //    Console.WriteLine("\n");
            //};

            Console.Read();
        }
    }
}
