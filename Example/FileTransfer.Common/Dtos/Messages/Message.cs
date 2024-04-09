namespace FileTransfer.Common.Dtos.Messages
{
    public class Message
    {
        public string MessageId { get; } = Guid.NewGuid().ToString();
        public DateTime SendTime { get; } = DateTime.Now;

        public Message()
        {
        }
    }
}
