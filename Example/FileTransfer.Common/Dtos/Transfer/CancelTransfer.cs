using FileTransfer.Common.Dtos.Messages;

namespace FileTransfer.Common.Dtos.Transfer
{
    //取消发送，不需要等待回复
    public class CancelTransfer : Message
    {
        public string FileSendId { get; set; }
    }
}
