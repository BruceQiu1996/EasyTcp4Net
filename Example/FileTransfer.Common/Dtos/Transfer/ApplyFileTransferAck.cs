using FileTransfer.Common.Dtos.Messages;

namespace FileTransfer.Common.Dtos.Transfer
{
    public class ApplyFileTransferAck : Message
    {
        public string FileSendId { get; set; }
        public ApplyFileTransferAckResult Result { get; set; }
        public string Message { get; set; }
        public long TransferedBytes { get; set; } = 0;
    }

    public enum ApplyFileTransferAckResult 
    {
        Approved = 1,
        Rejected = 2,
        TaskCompleted = 3,
        TaskExistAndWorking = 4,
        DataError = 5,
    }
}
