using FileTransfer.Models;
using Microsoft.EntityFrameworkCore;

namespace FileTransfer.Resources
{
    public class FileTransferDbContext : DbContext
    {
        public DbSet<RemoteChannelModel> RemoteChannels { get; set; }
        public DbSet<FileSendRecord> FileSendRecords { get; set; }

        public FileTransferDbContext(DbContextOptions<FileTransferDbContext> options) : base(options)
        {
        }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {

        }
    }
}
