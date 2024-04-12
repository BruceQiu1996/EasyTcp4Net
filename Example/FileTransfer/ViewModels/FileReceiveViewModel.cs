using CommunityToolkit.Mvvm.ComponentModel;
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
        public FileReceiveStatus Status { get; set; }
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

        public FileReceiveViewModel() { }

        public static FileReceiveViewModel FromModel(FileReceiveRecordModel model)
        {
            FileReceiveViewModel fileReceiveViewModel = new FileReceiveViewModel();
            fileReceiveViewModel.Id = model.Id;

            fileReceiveViewModel.FileName = model.FileName;
            fileReceiveViewModel.FileSendId = model.FileSendId;
            fileReceiveViewModel.TempFileLocation = model.TempFileSaveLocation;
            fileReceiveViewModel.Status = model.Status;
            fileReceiveViewModel.Size = model.TotalSize;
            fileReceiveViewModel.Progress = fileReceiveViewModel.Size == 0 ? 100 : fileReceiveViewModel.TransferBytes * 100 / fileReceiveViewModel.Size;
            fileReceiveViewModel.RemoteEndpoint = $"来自：{model.LastRemoteEndpoint}";
            fileReceiveViewModel.TransferToken = Guid.NewGuid().ToString();
            fileReceiveViewModel.Code = model.Code;

            return fileReceiveViewModel;
        }

        public async Task<(bool, string)> ReceiveDataAsync(FileSegement fileSegement)
        {
            (bool, string) result = (true, null);
            if (fileSegement.TransferToken != TransferToken || fileSegement.FileSendId != FileSendId)
                return (false, "Token或者ID错误");

            FileInfo fileInfo = new FileInfo(TempFileLocation);
            using (var fileStream = File.OpenWrite(TempFileLocation))
            {
                fileStream.Seek(fileInfo.Length, SeekOrigin.Begin);
                await fileStream.WriteAsync(fileSegement.Data);
                TransferBytes += fileSegement.Data.Length;
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

                }
            }

            return result;
        }

        public async Task NormolCompletedAsync()
        {
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
    }
}
