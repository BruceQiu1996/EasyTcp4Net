using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using FileTransfer.Models;
using FileTransfer.Resources;
using Microsoft.EntityFrameworkCore;
using System.Collections.ObjectModel;

namespace FileTransfer.ViewModels.Transfer
{
    public class CompleteTransferPageViewModel : ObservableObject
    {
        public ObservableCollection<FileTransferCompletedViewModel> FileTransferCompletedViewModels { get; set; }

        private readonly FileTransferDbContext _fileTransferDbContext;
        public CompleteTransferPageViewModel(FileTransferDbContext fileTransferDbContext)
        {
            FileTransferCompletedViewModels = new ObservableCollection<FileTransferCompletedViewModel>();
            _fileTransferDbContext = fileTransferDbContext;
            LoadCommandAsync = new AsyncRelayCommand(LoadAsync);
        }

        private bool _loaded = false;

        public AsyncRelayCommand LoadCommandAsync { get; set; }
        private async Task LoadAsync()
        {
            if (_loaded) return;
            FileTransferCompletedViewModels.Clear();
            var receivedRecords = await _fileTransferDbContext.FileReceiveRecords.Where(x =>
                x.Status == FileReceiveStatus.Faild || x.Status == FileReceiveStatus.Completed).ToListAsync();

            var sendRecords = await _fileTransferDbContext.FileSendRecords.Where(x =>
               x.Status == FileSendStatus.Faild || x.Status == FileSendStatus.Completed).ToListAsync();

            var list = new List<FileTransferCompletedViewModel>();
            foreach (var record in receivedRecords) 
            {
                list.Add(FileTransferCompletedViewModel.FromReceiveRecord(record));
            }
            foreach (var record in sendRecords)
            {
                list.Add(FileTransferCompletedViewModel.FromSendRecord(record));
            }

            foreach (var item in list.OrderByDescending(x => x.FinishTime))
            {
                FileTransferCompletedViewModels.Add(item);
            };

            _loaded = true;
        }
    }
}
