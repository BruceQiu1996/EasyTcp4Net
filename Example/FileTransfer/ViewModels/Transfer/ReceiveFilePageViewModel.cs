using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using CommunityToolkit.Mvvm.Messaging;
using FileTransfer.Common.Dtos.Transfer;
using FileTransfer.Models;
using FileTransfer.Resources;
using Microsoft.EntityFrameworkCore;
using System.Collections.ObjectModel;
using System.IO;
using System.Windows;

namespace FileTransfer.ViewModels.Transfer
{
    public class ReceiveFilePageViewModel : ObservableObject
    {
        private ObservableCollection<FileReceiveViewModel> fileReceiveViewModels = new ObservableCollection<FileReceiveViewModel>();
        public ObservableCollection<FileReceiveViewModel> FileReceiveViewModels
        {
            get => fileReceiveViewModels;
            set => SetProperty(ref fileReceiveViewModels, value);
        }

        public AsyncRelayCommand LoadCommandAsync { get; set; }

        private readonly FileTransferDbContext _fileTransferDbContext;
        public ReceiveFilePageViewModel(FileTransferDbContext fileTransferDbContext)
        {
            //从channel发过来的发送任务
            WeakReferenceMessenger.Default.Register<ReceiveFilePageViewModel,
                FileReceiveViewModel, string>(this, "AddReceiveFileRecord", async (x, y) =>
                {
                    Application.Current.Dispatcher.Invoke(() =>
                    {
                        FileReceiveViewModels.Add(y);
                    });
                });

            LoadCommandAsync = new AsyncRelayCommand(LoadAsync);
            _fileTransferDbContext = fileTransferDbContext;
        }

        private async Task LoadAsync()
        {
            FileReceiveViewModels.Clear();
            var records = await _fileTransferDbContext.FileReceiveRecordModels.Where(x =>
            x.Status != FileReceiveStatus.Faild && x.Status != FileReceiveStatus.Completed).ToListAsync();

            records.ForEach(x =>
            {
                FileReceiveViewModels.Add(FileReceiveViewModel.FromModel(x));
            });
        }
    }
}
