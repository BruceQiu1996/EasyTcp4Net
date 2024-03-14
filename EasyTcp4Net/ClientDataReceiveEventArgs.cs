namespace EasyTcp4Net
{
    public class ClientDataReceiveEventArgs
    {
        public ClientDataReceiveEventArgs(Memory<byte> data) 
        {
            Data = data.ToArray();
        }

        public byte[] Data { get; private set; }
    }
}
