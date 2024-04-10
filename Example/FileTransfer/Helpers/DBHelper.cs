using FileTransfer.Models;
using FileTransfer.Resources;
using Microsoft.EntityFrameworkCore;

namespace FileTransfer.Helpers
{
    /// <summary>
    /// 封装所有的写操作
    /// </summary>
    public class DBHelper
    {
        private readonly TimeSpan _timeout = TimeSpan.FromSeconds(5);
        private readonly Semaphore _semaphore = new Semaphore(1, 1); //sqlite需要单线程操作写
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

        public async Task<bool> AddFileReceiveRecordAsync(FileReceiveRecordModel fileReceiveRecord)
        {
            try
            {
                _semaphore.WaitOne(_timeout);
                await _fileTransferDbContext.FileReceiveRecordModels.AddAsync(fileReceiveRecord);
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
        /// 任务完成
        /// </summary>
        /// <param name="id">任务发送编号</param>
        /// <param name="remoteId">远端连接编号</param>
        /// <param name="result">结果</param>
        /// <param name="message">信息</param>
        /// <returns></returns>
        public async Task<bool> UpdateFileSendRecordCompleteAsync(string id, string remoteId, bool result, string message = null)
        {
            try
            {
                _semaphore.WaitOne(_timeout);
                var record =
                    await _fileTransferDbContext.FileSendRecords.FirstOrDefaultAsync(x => x.Id == id
                    && x.RemoteId == remoteId);
                if (record != null)
                {
                    record.Status = result ? FileSendStatus.Completed : FileSendStatus.Faild;
                    record.Message = message;
                    _fileTransferDbContext.Update(record);
                    await _fileTransferDbContext.SaveChangesAsync();
                }

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
