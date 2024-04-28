using System.ComponentModel;
using System.ComponentModel.DataAnnotations;

namespace FileTransfer.Models
{
    public class FileSendRecordModel
    {
        [Key]
        public string Id { get; set; } = Guid.NewGuid().ToString();
        [Required]
        public string FileLocation { get; set; }
        [Required]
        public string FileName { get; set; }
        //总字节
        public long TotalSize { get; set; }
        //已传输字节
        public long TransferedSize { get; set; }
        public string Code { get; set; }
        public string RemoteId { get; set; }
        public DateTime CreateTime { get; set; } = DateTime.Now;
        public DateTime? FinishTime { get; set; }
        public FileSendStatus Status { get; set; }
        public string? Message { get; set; }

        public FileSendRecordModel() { }

        public FileSendRecordModel(string location, string fileName, string code, long totalSize, string remote)
        {
            FileName = fileName;
            FileLocation = location;
            Code = code;
            RemoteId = remote;
            TotalSize = totalSize;
            Status = FileSendStatus.Pending;
        }
    }

    public enum FileSendStatus
    {
        [Description("等待中")]
        Pending = 1,//等待传输
        [Description("传输中")]
        Transfering = 2, //传输中
        [Description("已完成")]
        Completed = 3, //完成
        [Description("失败")]
        Faild = 4, //传输失败
        [Description("暂停中")]
        Pausing = 5,//暂停中
    }
}
