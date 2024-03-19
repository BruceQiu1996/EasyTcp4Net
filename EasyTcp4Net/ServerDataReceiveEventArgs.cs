namespace EasyTcp4Net
{
    public class ServerDataReceiveEventArgs
    {
        public ClientSession Session { get; private set; }
        public Memory<byte> Data { get; set; }
        public ServerDataReceiveEventArgs(ClientSession clientSession, Memory<byte> packet)
        {
            Session = clientSession;
            Data = packet;
        }
    }
}
