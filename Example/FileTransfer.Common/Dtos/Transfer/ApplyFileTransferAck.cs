using FileTransfer.Common.Dtos.Messages;

namespace FileTransfer.Common.Dtos.Transfer
{
    public class ApplyFileTransferAck : Message
    {
        public string FileSendId { get; set; }
        public bool Approve { get; set; }
        public string Message { get; set; }
        public string Token { get; set; } //本次传输的Token,如果是断点续传则需要发送端携带过来，目的是方便确定文件的唯一性
    }
}
