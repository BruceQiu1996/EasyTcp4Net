using System.ComponentModel.DataAnnotations;

namespace FileTransfer.Models
{
    public class RemoteChannelModel
    {
        [Key]
        public string Id { get; set; } = Guid.NewGuid().ToString();
        public string? Remark { get; set; }
        public string IPAddress { get; set; }
        public ushort Port { get; set; }
        public DateTime CreateTime { get; set; } = DateTime.Now;
    }
}
