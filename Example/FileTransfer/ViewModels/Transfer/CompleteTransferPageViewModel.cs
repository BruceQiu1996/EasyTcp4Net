using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using CommunityToolkit.Mvvm.Messaging;
using FileTransfer.Helpers;
using FileTransfer.Models;
using System.Collections.ObjectModel;
using System.Windows;

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
            WeakReferenceMessenger.Default.Register<CompleteTransferPageViewModel,
               Tuple<string, string>, string>(this, "AddToCompletePage", async (x, y) =>
               {
                   FileTransferCompletedViewModel fileTransferCompletedViewModel = null;
                   if (y.Item1 == "receive")
                   {
                       var record = await _dbHelper.FindAsync<FileReceiveRecordModel>(y.Item2);
                       fileTransferCompletedViewModel = FileTransferCompletedViewModel.FromReceiveRecord(record);
                   }
                   else
                   {
                       var record = await _dbHelper.FindAsync<FileSendRecordModel>(y.Item2);
                       fileTransferCompletedViewModel = FileTransferCompletedViewModel.FromSendRecord(record);
                   }
                   Application.Current.Dispatcher.Invoke(() =>
                   {
                       AddRecordViewModel(fileTransferCompletedViewModel, 0);
                   });
               });
        }

        private bool _hasData;
        public bool HasData
        {
            get => _hasData;
            set => SetProperty(ref _hasData, value);
        }

        private bool _loaded = false;

        public AsyncRelayCommand LoadCommandAsync { get; set; }
        private async Task LoadAsync()
        {
            if (_loaded) return;
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
                AddRecordViewModel(item);
            };

            _loaded = true;
        }

        public void AddRecordViewModel(FileTransferCompletedViewModel fileReceiveViewModel, int insertIndex = -1)
        {
            if (insertIndex == -1)
                FileTransferCompletedViewModels.Add(fileReceiveViewModel);
            else if (insertIndex >= 0)
            {
                FileTransferCompletedViewModels.Insert(insertIndex, fileReceiveViewModel);
            }
            HasData = FileTransferCompletedViewModels.Count > 0;
        }

        public void RemoveRecordViewModel(FileTransferCompletedViewModel fileReceiveViewModel)
        {
            FileTransferCompletedViewModels.Remove(fileReceiveViewModel);
            HasData = FileTransferCompletedViewModels.Count > 0;
        }
    }
}
