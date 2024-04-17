﻿using System.ComponentModel.DataAnnotations;

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
        Pending = 1,//等待传输
        Transfering = 2, //传输中
        Completed = 3, //完成
        Faild = 4, //传输失败
        Pausing = 5,//暂停中
    }
}