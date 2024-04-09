using System.ComponentModel.DataAnnotations;

namespace FileTransfer.Models
{
    public class FileSendRecord
    {
        [Key]
        public string Id { get; set; } = Guid.NewGuid().ToString();
        [Required]
        public string FileLocation { get; set; }
        public string Code { get; set; }
        public string RemoteId { get; set; }
        public string? TransferToken { get; set; }
        public DateTime CreateTime { get; set; } = DateTime.Now;
        public DateTime? FinishTime { get; set; }
        public FileSendStatus Status { get; set; }
        public string? Message { get; set; }

        public FileSendRecord()
        {

        }

        public FileSendRecord(string location, string code, string remote)
        {
            FileLocation = location;
            Code = code;
            RemoteId = remote;
            Status = FileSendStatus.Pending;
        }
    }

    public enum FileSendStatus
    {
        Pending = 1,//等待传输
        Transfering = 2, //传输中
        Completed = 3, //完成
        Faild = 4, //传输失败
    }
}
