using FileTransfer.Common.Dtos.Messages;

namespace FileTransfer.Common.Dtos.Transfer
{
    /// <summary>
    /// 发送文件的块
    /// </summary>
    public class FileSegement : Message
    {
        public int TotalSegement { get; set; }
        public int SegementIndex { get; set; }
        public string FileSendId { get; set; }
        public string TransferToken { get; set; }
        public ReadOnlyMemory<byte> Data { get; set; }
    }
}
