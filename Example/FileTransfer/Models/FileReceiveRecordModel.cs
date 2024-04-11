using System.ComponentModel.DataAnnotations;

namespace FileTransfer.Models
{
    /// <summary>
    /// 收到文件的记录
    /// </summary>
    public class FileReceiveRecordModel
    {
        [Key]
        public string Id { get; set; } = Guid.NewGuid().ToString();
        public string? TempFileSaveLocation { get; set; }
        public string? FileSaveLocation { get; set; }
        [Required]
        public string FileSendId { get; set; }
        [Required]
        public string LastRemoteEndpoint { get; set; }
        [Required]
        public string FileName { get; set; }
        //总字节
        public long TotalSize { get; set; }
        public string Code { get; set; }
        public DateTime CreateTime { get; set; } = DateTime.Now;
        public DateTime? FinishTime { get; set; }
        public FileReceiveStatus Status { get; set; }
        public string? Message { get; set; }

        public FileReceiveRecordModel()
        {

        }

        public FileReceiveRecordModel(string fileName, string code, long totalSize, string tempfileSaveLocation, string fileSendId, string lastRemoteEndpoint)
        {
            FileName = fileName;
            Code = code;
            TotalSize = totalSize;
            Status = FileReceiveStatus.Transfering;
            TempFileSaveLocation = tempfileSaveLocation;
            FileSendId = fileSendId;
            LastRemoteEndpoint = lastRemoteEndpoint;
        }
    }

    public enum FileReceiveStatus
    {
        Transfering = 1, //传输中
        Completed = 2, //完成
        Faild = 3, //传输失败
    }
}
