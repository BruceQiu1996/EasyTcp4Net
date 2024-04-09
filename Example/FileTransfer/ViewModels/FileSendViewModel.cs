using CommunityToolkit.Mvvm.ComponentModel;
using FileTransfer.Helpers;
using FileTransfer.Models;
using Microsoft.Extensions.DependencyInjection;
using System.Windows.Media.Imaging;

namespace FileTransfer.ViewModels
{
    public class FileSendViewModel : ObservableObject
    {
        public string FileName { get; set; }
        public FileSendStatus Status { get; set; }
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

        public FileSendViewModel() { }

        public static FileSendViewModel FromModel(FileSendRecordModel model)
        {
            FileSendViewModel fileSendViewModel = new FileSendViewModel();
            fileSendViewModel.FileName = model.FileName;
            fileSendViewModel.Status = model.Status;
            fileSendViewModel.Size = model.TotalSize;
            fileSendViewModel.TransferBytes = model.TransferedSize;
            fileSendViewModel.Progress = fileSendViewModel.Size == 0 ? 100 : fileSendViewModel.TransferBytes * 100 / fileSendViewModel.Size;

            return fileSendViewModel;
        }
    }
}
