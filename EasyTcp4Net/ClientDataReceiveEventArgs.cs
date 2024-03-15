namespace EasyTcp4Net
{
    public class ClientDataReceiveEventArgs
    {
        public ClientDataReceiveEventArgs(Packet packet) 
        {
            Data = packet;
        }

        public Packet Data { get; private set; }
    }
}
