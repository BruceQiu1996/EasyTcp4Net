﻿using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Messaging;
using FileTransfer.Common.Dtos.Transfer;
using FileTransfer.Helpers;
using FileTransfer.Models;
using Microsoft.Extensions.DependencyInjection;
using System.IO;
using System.Windows.Media.Imaging;

namespace FileTransfer.ViewModels
{
    public class FileReceiveViewModel : ObservableObject
    {
        public string Id { get; set; }
        public string RemoteEndpoint { get; set; }
        public string TransferToken { get; private set; }
        public string FileName { get; set; }
        public string TempFileLocation { get; set; }

        private FileReceiveStatus _status;
        public FileReceiveStatus Status
        {
            get => _status;
            set
            {
                SetProperty(ref _status, value);
                Pausing = Status == FileReceiveStatus.Pausing;
                StatusMessage = Status.GetDescription();
            }
        }

        private bool pausing;
        public bool Pausing
        {
            get => pausing;
            set
            {
                SetProperty(ref pausing, value);
            }
        }

        private string _statusMessage;
        public string StatusMessage
        {
            get => _statusMessage;
            set
            {
                SetProperty(ref _statusMessage, value);
            }
        }

        public BitmapImage Icon => App.ServiceProvider!.GetRequiredService<FileHelper>().GetIconByFileExtension(FileName).Item2;
        public long Size { get; set; } //发送文件的大小
        private long _transferBytes;
        //已经传输的字节
        public long TransferBytes
        {
            get => _transferBytes;
            set
            {
                _transferBytes = value;
                Progress = Size == 0 ? 100 : TransferBytes * 100 / Size;
            }
        }
        public string SizeText => App.ServiceProvider!.GetRequiredService<FileHelper>().ToSizeText(Size);
        private double _progress;
        //传输的进度
        public double Progress
        {
            get => _progress;
            set => SetProperty(ref _progress, value);
        }

        private string _speed;
        public string Speed
        {
            get => _speed;
            set => SetProperty(ref _speed, value);
        }

        public string FileSendId { get; private set; }
        public string Code { get; private set; }

        public FileReceiveViewModel(FileReceiveRecordModel model)
        {
            Id = model.Id;
            FileName = model.FileName;
            FileSendId = model.FileSendId;
            TempFileLocation = model.TempFileSaveLocation;
            Status = model.Status;
            Size = model.TotalSize;
            if (File.Exists(model.TempFileSaveLocation))
            {
                FileInfo fileInfo = new FileInfo(model.TempFileSaveLocation);
                TransferBytes = fileInfo.Length;
            }
            Progress = Size == 0 ? 100 : TransferBytes * 100 / Size;
            RemoteEndpoint = $"来自：{model.LastRemoteEndpoint}";
            TransferToken = Guid.NewGuid().ToString();
            Code = model.Code;
        }

        private readonly SemaphoreSlim _semaphore = new SemaphoreSlim(1, 1);
        public async Task<(bool, string)> ReceiveDataAsync(FileSegement fileSegement)
        {
            (bool, string) result = (false, null);
            if (fileSegement.FileSendId != FileSendId)
                return (false, "传输任务ID错误");
            try
            {
                _semaphore.Wait();
                FileInfo fileInfo = new FileInfo(TempFileLocation);
                using (var fileStream = File.OpenWrite(TempFileLocation))
                {
                    fileStream.Seek(fileInfo.Length, SeekOrigin.Begin);
                    await fileStream.WriteAsync(fileSegement.Data);
                    TransferBytes += fileSegement.Data.Length;
                }
            }
            catch (Exception ex) 
            {
                await ErrorCompletedAsync("接收文件异常");
            }
            finally
            {
                _semaphore.Release();
            }

            if (fileSegement.TotalSegement == fileSegement.SegementIndex)
            {
                //发送完成判断sha265
                string code = null; //sha265
                using (var fileStream = new FileStream(TempFileLocation, FileMode.Open, FileAccess.Read))
                {
                    code = App.ServiceProvider!.GetRequiredService<FileHelper>().ToSHA256(fileStream);
                }

                if (code == Code)
                {
                    //TODO异常结束
                    await NormolCompletedAsync();
                }
                else
                {
                    await ErrorCompletedAsync("文件内容异常");
                }


                return (true,null);
            }

            return result;
        }

        /// <summary>
        /// 正常完成接收文件
        /// </summary>
        /// <returns></returns>
        public async Task NormolCompletedAsync()
        {
            if (Status == FileReceiveStatus.Completed || Status == FileReceiveStatus.Faild)
                return;

            Status = FileReceiveStatus.Completed;
            var fileHelper = App.ServiceProvider!.GetRequiredService<FileHelper>();
            var dbHelper = App.ServiceProvider!.GetRequiredService<DBHelper>();
            //生成新文件
            var canMove = true;
            var newFilePath = fileHelper.GetAvailableFileLocation(FileName, Path.GetDirectoryName(TempFileLocation)!);
            try
            {
                File.Move(TempFileLocation, newFilePath);
            }
            catch (Exception ex)
            {
                //TODO日志
                canMove = false;
            }
            //更新数据库
            await dbHelper.UpdateFileReceiveRecordCompleteAsync(Id, newFilePath, canMove, canMove ? null : "文件保存错误");
            WeakReferenceMessenger.Default.Send(Id, "ReceiveFinish");
        }

        /// <summary>
        /// 正常完成接收文件
        /// </summary>
        /// <returns></returns>
        public async Task ErrorCompletedAsync(string message)
        {
            if (Status == FileReceiveStatus.Completed || Status == FileReceiveStatus.Faild)
                return;

            Status = FileReceiveStatus.Faild;
            var dbHelper = App.ServiceProvider!.GetRequiredService<DBHelper>();
            //更新数据库
            await dbHelper.UpdateFileReceiveRecordCompleteAsync(Id, null, false, message);
            int i = 0;
            while (i < 3)
            {
                try
                {
                    if (File.Exists(TempFileLocation))
                    {
                        File.Delete(TempFileLocation);
                    }

                    break;
                }
                catch (Exception ex)
                {
                    await Task.Delay(100);
                    i++;
                    continue;
                }
            }

            WeakReferenceMessenger.Default.Send(Id, "ReceiveFinish");
        }

        /// <summary>
        /// 被动取消接收任务
        /// </summary>
        /// <returns></returns>
        public async Task PassiveCancelAsync(CancelTransfer cancelTransfer)
        {
            if (cancelTransfer.FileSendId != FileSendId)
                return;

            await ErrorCompletedAsync("发送端取消发送");
        }
    }
}
