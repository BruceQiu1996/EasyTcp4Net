namespace EasyTcp4Net
{
    public class ClientDataReceiveEventArgs
    {
        public ClientDataReceiveEventArgs(Memory<byte> packet) 
        {
            Data = packet;
        }

        public Memory<byte> Data { get; private set; }
    }
}
