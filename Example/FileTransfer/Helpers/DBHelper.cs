using FileTransfer.Models;
using FileTransfer.Resources;
using Microsoft.EntityFrameworkCore;
using System.Linq.Expressions;

namespace FileTransfer.Helpers
{
    /// <summary>
    /// 封装所有的写操作
    /// </summary>
    public class DBHelper
    {
        private readonly TimeSpan _timeout = TimeSpan.FromSeconds(5);
        private readonly SemaphoreSlim _semaphore = new SemaphoreSlim(1); //sqlite需要单线程操作写
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
                _semaphore.Wait(_timeout);
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
                _semaphore.Wait(_timeout);
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
                _semaphore.Wait(_timeout);
                await _fileTransferDbContext.FileReceiveRecords.AddAsync(fileReceiveRecord);
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
        /// 发送任务完成
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
                _semaphore.Wait(_timeout);
                var record =
                    await _fileTransferDbContext.FileSendRecords.FirstOrDefaultAsync(x => x.Id == id
                    && x.RemoteId == remoteId);
                if (record != null)
                {
                    record.Status = result ? FileSendStatus.Completed : FileSendStatus.Faild;
                    record.Message = message;
                    record.FinishTime = DateTime.Now;
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

        /// <summary>
        /// 接收文件完成
        /// </summary>
        /// <param name="id">任务发送编号</param>
        /// <param name="path">文件存放路径位置</param>
        /// <param name="result">结果</param>
        /// <param name="message">信息</param>
        /// <returns></returns>
        public async Task<bool> UpdateFileReceiveRecordCompleteAsync(string id, string path, bool result, string message = null)
        {
            try
            {
                _semaphore.Wait(_timeout);
                var record =
                    await _fileTransferDbContext.FileReceiveRecords.FirstOrDefaultAsync(x => x.Id == id);

                if (record != null)
                {
                    record.Status = result ? FileReceiveStatus.Completed : FileReceiveStatus.Faild;
                    record.FileSaveLocation = path;
                    record.FinishTime = DateTime.Now;
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

        /// <summary>
        /// 按照id查询
        /// </summary>
        /// <typeparam name="TModel"></typeparam>
        /// <param name="id"></param>
        /// <returns>对象</returns>
        public async Task<TModel> FindAsync<TModel>(string id) where TModel : class
        {
            try
            {
                _semaphore.Wait(_timeout);
                var record = await _fileTransferDbContext.FindAsync<TModel>(id);

                return record;
            }
            catch
            {
                return null;
            }
            finally
            {
                _semaphore.Release();
            }
        }

        /// <summary>
        /// 获取发送记录包含远程端信息
        /// </summary>
        /// <returns></returns>
        public async Task<List<FileSendWithRemoteChannelModel>> GetSendRecordsWithRemoteChannelAsync()
        {
            try
            {
                _semaphore.Wait(_timeout);
                var records = await _fileTransferDbContext.FileSendRecords
                .Join(_fileTransferDbContext.RemoteChannels, x => x.RemoteId,
                x => x.Id, (x, y) =>
                new FileSendWithRemoteChannelModel
                {
                    FileSendRecordModel = x,
                    RemoteChannelModel = y
                }).Where(x => x.FileSendRecordModel.Status != FileSendStatus.Completed
                && x.FileSendRecordModel.Status != FileSendStatus.Faild).OrderByDescending(x => x.FileSendRecordModel.CreateTime).ToListAsync();

                return records;
            }
            catch
            {
                return null;
            }
            finally
            {
                _semaphore.Release();
            }

        }

        /// <summary>
        /// 获取所有的
        /// </summary>
        /// <typeparam name="TModel"></typeparam>
        /// <returns></returns>
        public async Task<List<TModel>> GetAllAsync<TModel>() where TModel : class
        {
            try
            {
                _semaphore.Wait(_timeout);
                var records = await _fileTransferDbContext.Set<TModel>().ToListAsync();

                return records;
            }
            catch
            {
                return null;
            }
            finally
            {
                _semaphore.Release();
            }
        }

        /// <summary>
        /// 筛选元素
        /// </summary>
        /// <typeparam name="模型类型"></typeparam>
        /// <param name="expression">筛选条件</param>
        /// <returns></returns>
        public async Task<List<TModel>> WhereAsync<TModel>(Expression<Func<TModel, bool>> expression) where TModel : class
        {
            try
            {
                _semaphore.Wait(_timeout);
                var records = await _fileTransferDbContext.Set<TModel>().Where(expression).ToListAsync();

                return records;
            }
            catch
            {
                return null;
            }
            finally
            {
                _semaphore.Release();
            }
        }

        /// <summary>
        /// 筛选第一个元素
        /// </summary>
        /// <typeparam name="模型类型"></typeparam>
        /// <param name="expression">筛选条件</param>
        /// <returns></returns>
        public async Task<TModel> FirstOrDefaultAsync<TModel>(Expression<Func<TModel, bool>> expression) where TModel : class
        {
            try
            {
                _semaphore.Wait(_timeout);
                var record = await _fileTransferDbContext.Set<TModel>().FirstOrDefaultAsync(expression);

                return record;
            }
            catch
            {
                return null;
            }
            finally
            {
                _semaphore.Release();
            }
        }
    }
}
