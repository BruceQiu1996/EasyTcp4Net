using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using FileTransfer.Helpers;
using FileTransfer.Models;
using System.Collections.ObjectModel;

namespace FileTransfer.ViewModels.Transfer
{
    public class CompleteTransferPageViewModel : ObservableObject
    {
        public ObservableCollection<FileTransferCompletedViewModel> FileTransferCompletedViewModels { get; set; }

        private readonly DBHelper _dbHelper;
        public CompleteTransferPageViewModel(DBHelper dbHelper)
        {
            FileTransferCompletedViewModels = new ObservableCollection<FileTransferCompletedViewModel>();
            _dbHelper = dbHelper;
            LoadCommandAsync = new AsyncRelayCommand(LoadAsync);
        }

        private bool _loaded = false;

        public AsyncRelayCommand LoadCommandAsync { get; set; }
        private async Task LoadAsync()
        {
            if (_loaded) return;
            FileTransferCompletedViewModels.Clear();
            var receivedRecords = await _dbHelper.WhereAsync<FileReceiveRecordModel>(x =>
                x.Status == FileReceiveStatus.Faild || x.Status == FileReceiveStatus.Completed);

            var sendRecords = await _dbHelper.WhereAsync<FileSendRecordModel>(x =>
               x.Status == FileSendStatus.Faild || x.Status == FileSendStatus.Completed);

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
