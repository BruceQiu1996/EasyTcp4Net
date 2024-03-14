namespace EasyTcp4Net
{
    public class ServerDataReceiveEventArgs
    {
        public ClientSession Session { get; private set; }
        public byte[] Data { get; set; }
        public ServerDataReceiveEventArgs(ClientSession clientSession, Memory<byte> data)
        {
            Session = clientSession;
            Data = data.ToArray();
        }
    }
}
