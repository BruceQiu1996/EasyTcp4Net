using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using CommunityToolkit.Mvvm.Messaging;
using FileTransfer.Helpers;
using FileTransfer.Models;
using System.Collections.ObjectModel;
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

        private bool _hasData;
        public bool HasData
        {
            get => _hasData;
            set => SetProperty(ref _hasData, value);
        }

        public AsyncRelayCommand LoadCommandAsync { get; set; }

        private readonly DBHelper _dBHelper;
        public ReceiveFilePageViewModel(DBHelper dBHelper)
        {
            //从channel发过来的发送任务
            WeakReferenceMessenger.Default.Register<ReceiveFilePageViewModel,
                FileReceiveViewModel, string>(this, "AddReceiveFileRecord", async (x, y) =>
                {
                    Application.Current.Dispatcher.Invoke(() =>
                    {
                        AddRecordViewModel(y, 0);
                    });
                });

            WeakReferenceMessenger.Default.Register<ReceiveFilePageViewModel,
                string, string>(this, "Load", async (x, y) =>
                {
                    await LoadAsync();
                });

            WeakReferenceMessenger.Default.Register<ReceiveFilePageViewModel,
               string, string>(this, "ReceiveFinish", async (x, y) =>
               {
                   Application.Current.Dispatcher.Invoke(() =>
                   {
                       var viewModel = FileReceiveViewModels.FirstOrDefault(x => x.Id == y);
                       if (viewModel != null)
                       {
                           RemoveRecordViewModel(viewModel);
                           //增加到完成界面
                           WeakReferenceMessenger.Default.Send(new Tuple<string, string>("receive", y), "AddToCompletePage");
                       }
                   });
               });

            LoadCommandAsync = new AsyncRelayCommand(LoadAsync);
            _dBHelper = dBHelper;
        }

        private bool _loaded = false;
        private async Task LoadAsync()
        {
            if (_loaded) return;
            FileReceiveViewModels.Clear();
            var records = await _dBHelper.WhereAsync<FileReceiveRecordModel>(x =>
            x.Status != FileReceiveStatus.Faild && x.Status != FileReceiveStatus.Completed);

            records.ForEach(x =>
            {
                AddRecordViewModel(new FileReceiveViewModel(x));
            });

            _loaded = true;
        }

        public void AddRecordViewModel(FileReceiveViewModel fileReceiveViewModel, int insertIndex = -1)
        {
            if (insertIndex == -1)
                FileReceiveViewModels.Add(fileReceiveViewModel);
            else if (insertIndex >= 0)
            {
                FileReceiveViewModels.Insert(insertIndex, fileReceiveViewModel);
            }
            HasData = FileReceiveViewModels.Count > 0;
            WeakReferenceMessenger.Default.Send(new Tuple<string, int>(null, FileReceiveViewModels.Count), "TransferReceiveCount");
        }

        public void RemoveRecordViewModel(FileReceiveViewModel fileReceiveViewModel)
        {
            FileReceiveViewModels.Remove(fileReceiveViewModel);
            HasData = FileReceiveViewModels.Count > 0;
            WeakReferenceMessenger.Default.Send(new Tuple<string, int>(null, FileReceiveViewModels.Count), "TransferReceiveCount");
        }
    }
}
