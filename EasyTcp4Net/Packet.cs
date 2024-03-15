using System.Net;

namespace EasyTcp4Net
{
    public class Packet
    {
        public Memory<byte> Data { get; set; }
        public IPEndPoint From { get; set; }
    }
}
