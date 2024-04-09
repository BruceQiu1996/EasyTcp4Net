namespace FileTransfer.Common.Dtos.Messages
{
    public enum MessageType : int
    {
        ConnectionAck = 1, //连接后的回复

        ApplyTrasnfer = 2,
        ApplyTrasnferAck = 3,
    }
}
