using CommunityToolkit.Mvvm.ComponentModel;
using FileTransfer.Helpers;
using FileTransfer.Models;
using Microsoft.Extensions.DependencyInjection;
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
                Progress = TransferBytes / (double)Size * 100;
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

        public FileReceiveViewModel() { }

        public static FileReceiveViewModel FromModel(FileReceiveRecordModel model)
        {
            FileReceiveViewModel fileReceiveViewModel = new FileReceiveViewModel();
            fileReceiveViewModel.Id = model.Id;
            fileReceiveViewModel.FileName = model.FileName;
            fileReceiveViewModel.TempFileLocation = model.TempFileSaveLocation;
            fileReceiveViewModel.Status = model.Status;
            fileReceiveViewModel.Size = model.TotalSize;
            fileReceiveViewModel.Progress = fileReceiveViewModel.Size == 0 ? 100 : fileReceiveViewModel.TransferBytes * 100 / fileReceiveViewModel.Size;
            fileReceiveViewModel.RemoteEndpoint = model.LastRemoteEndpoint;

            return fileReceiveViewModel;
        }
    }
}
