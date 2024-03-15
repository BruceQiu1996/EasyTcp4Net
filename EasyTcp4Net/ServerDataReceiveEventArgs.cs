namespace EasyTcp4Net
{
    public class ServerDataReceiveEventArgs
    {
        public ClientSession Session { get; private set; }
        public Packet Data { get; set; }
        public ServerDataReceiveEventArgs(ClientSession clientSession, Packet packet)
        {
            Session = clientSession;
            Data = packet;
        }
    }
}
