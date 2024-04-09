using FileTransfer.Models;
using FileTransfer.Resources;

namespace FileTransfer.Helpers
{
    /// <summary>
    /// 封装所有的写操作
    /// </summary>
    public class DBHelper
    {
        private readonly TimeSpan _timeout = TimeSpan.FromSeconds(5);
        private readonly Semaphore _semaphore= new Semaphore(1,1); //sqlite需要单线程操作写
        private readonly FileTransferDbContext _fileTransferDbContext;
        public DBHelper(FileTransferDbContext fileTransferDbContext)
        {
            _fileTransferDbContext = fileTransferDbContext;
        }

        /// <summary>
        /// 添加远端配置
        /// </summary>
        /// <param name="channelModel">远端模型</param>
        /// <returns></returns>
        public async Task<bool> AddRemoteChannelAsync(RemoteChannelModel channelModel) 
        {
            try
            {
                _semaphore.WaitOne(_timeout);
                await _fileTransferDbContext.RemoteChannels.AddAsync(channelModel);
                await _fileTransferDbContext.SaveChangesAsync();

                return true;
            }
            catch
            {
                return false;
            }
            finally
            {
                _semaphore.Release();
            }
        }

        /// <summary>
        /// 添加发送文件记录
        /// </summary>
        /// <param name="channelModel">远端模型</param>
        /// <returns></returns>
        public async Task<bool> AddFileSendRecordAsync(FileSendRecordModel fileSendRecord)
        {
            try
            {
                _semaphore.WaitOne(_timeout);
                await _fileTransferDbContext.FileSendRecords.AddAsync(fileSendRecord);
                await _fileTransferDbContext.SaveChangesAsync();

                return true;
            }
            catch
            {
                return false;
            }
            finally
            {
                _semaphore.Release();
            }
        }
    }
}
