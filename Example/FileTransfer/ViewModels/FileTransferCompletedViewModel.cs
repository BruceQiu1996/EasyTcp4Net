using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using FileTransfer.Helpers;
using FileTransfer.Models;
using Microsoft.Extensions.DependencyInjection;
using System.Windows.Media.Imaging;

namespace FileTransfer.ViewModels
{
    public class FileTransferCompletedViewModel : ObservableObject
    {
        public string FileName { get; set; }
        public long Size { get; set; }
        public string SizeText => App.ServiceProvider!.GetRequiredService<FileHelper>().ToSizeText(Size);
        public BitmapImage Icon => App.ServiceProvider!.GetRequiredService<FileHelper>().GetIconByFileExtension(FileName).Item2;
        public DateTime FinishTime { get; set; }
        public string FinishTimeText => FinishTime.ToString("yyyy-MM-dd HH:mm:ss");
        public string Direction { get; set; }
        public string Message { get; set; }
        public bool Success { get; set; }

        public AsyncRelayCommand OpenFileCommand { get; set; }
        public AsyncRelayCommand OpenFileLocationCommand { get; set; }

        public FileTransferCompletedViewModel()
        {
            OpenFileCommand = new AsyncRelayCommand(OpenFileAsync);
            OpenFileLocationCommand = new AsyncRelayCommand(OpenFileLocationAsync);
        }

        public static FileTransferCompletedViewModel FromSendRecord(FileSendRecordModel fileSendRecordModel)
        {
            return new FileTransferCompletedViewModel()
            {
                FileName = fileSendRecordModel.FileName,
                Size = fileSendRecordModel.TotalSize,
                FinishTime = fileSendRecordModel.FinishTime == null ? DateTime.MaxValue : fileSendRecordModel.FinishTime.Value,
                Direction = "发送",
                Message = fileSendRecordModel.Status == FileSendStatus.Completed ? "已完成" : fileSendRecordModel.Message,
                Success = fileSendRecordModel.Status == FileSendStatus.Completed
            };
        }

        public static FileTransferCompletedViewModel FromReceiveRecord(FileReceiveRecordModel fileReceiveRecordModel)
        {
            return new FileTransferCompletedViewModel()
            {
                FileName = fileReceiveRecordModel.FileName,
                Size = fileReceiveRecordModel.TotalSize,
                FinishTime = fileReceiveRecordModel.FinishTime == null ? DateTime.MaxValue : fileReceiveRecordModel.FinishTime.Value,
                Direction = "接收",
                Message = fileReceiveRecordModel.Status == FileReceiveStatus.Completed ? "已完成" : fileReceiveRecordModel.Message,
                Success = fileReceiveRecordModel.Status == FileReceiveStatus.Completed
            };
        }

        private async Task OpenFileAsync() 
        {
            
        }

        private async Task OpenFileLocationAsync() 
        {
        
        }
    }
}
