using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using CommunityToolkit.Mvvm.Messaging;
using FileTransfer.Helpers;
using System.Collections.ObjectModel;
using System.Windows;

namespace FileTransfer.ViewModels.Transfer
{
    public class SendFilePageViewModel : ObservableObject
    {
        public ObservableCollection<FileSendViewModel> FileSendViewModels { get; set; }
        public AsyncRelayCommand LoadCommandAsync { get; set; }

        private bool _hasData;
        public bool HasData
        {
            get => _hasData;
            set => SetProperty(ref _hasData, value);
        }

        private readonly DBHelper _dBHelper;
        public SendFilePageViewModel(DBHelper dBHelper)
        {
            //从channel发过来的发送任务
            WeakReferenceMessenger.Default.Register<SendFilePageViewModel,
                FileSendViewModel, string>(this, "AddSendFileRecord", async (x, y) =>
                {
                    Application.Current.Dispatcher.Invoke(() =>
                    {
                        AddRecordViewModel(y, 0);
                    });
                });

            WeakReferenceMessenger.Default.Register<SendFilePageViewModel,
                string, string>(this, "Load", async (x, y) =>
                {
                    await LoadAsync();
                });

            WeakReferenceMessenger.Default.Register<SendFilePageViewModel,
                string, string>(this, "SendFinish", async (x, y) =>
                {
                    Application.Current.Dispatcher.Invoke(() =>
                    {
                        var viewModel = FileSendViewModels!.FirstOrDefault(x => x.Id == y);
                        if (viewModel != null)
                        {
                            RemoveRecordViewModel(viewModel);
                            //增加到完成界面
                            WeakReferenceMessenger.Default.Send(new Tuple<string, string>("send", y), "AddToCompletePage");
                        }
                    });
                });

            FileSendViewModels = new ObservableCollection<FileSendViewModel>();
            LoadCommandAsync = new AsyncRelayCommand(LoadAsync);
            _dBHelper = dBHelper;
        }

        private bool _loaded = false;
        private async Task LoadAsync()
        {
            if (_loaded) return;
            await _dBHelper.UpdateFileSendRecordsUnCompleteToPauseAsync();
            var records = await _dBHelper.GetSendRecordsWithRemoteChannelAsync();

            records.ForEach(x =>
            {
                AddRecordViewModel(new FileSendViewModel(x.FileSendRecordModel, x.RemoteChannelModel));
            });

            _loaded = true;
        }

        public void AddRecordViewModel(FileSendViewModel fileSendViewModel, int insertIndex = -1)
        {
            if (insertIndex == -1)
                FileSendViewModels.Add(fileSendViewModel);
            else if (insertIndex >= 0)
            {
                FileSendViewModels.Insert(insertIndex, fileSendViewModel);
            }
            HasData = FileSendViewModels.Count > 0;
            WeakReferenceMessenger.Default.Send(new Tuple<string, int>(null, FileSendViewModels.Count), "TransferSendCount");
        }

        public void RemoveRecordViewModel(FileSendViewModel fileSendViewModel)
        {
            FileSendViewModels.Remove(fileSendViewModel);
            HasData = FileSendViewModels.Count > 0;
            WeakReferenceMessenger.Default.Send(new Tuple<string, int>(null, FileSendViewModels.Count), "TransferSendCount");
        }
    }
}
