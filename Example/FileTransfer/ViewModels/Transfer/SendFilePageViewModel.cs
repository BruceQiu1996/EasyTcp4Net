using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using CommunityToolkit.Mvvm.Messaging;
using EasyTcp4Net;
using FileTransfer.Models;
using FileTransfer.Resources;
using Microsoft.EntityFrameworkCore;
using System.Collections.ObjectModel;
using System.Windows;

namespace FileTransfer.ViewModels.Transfer
{
    public class SendFilePageViewModel : ObservableObject
    {
        public ObservableCollection<FileSendViewModel> FileSendViewModels { get; set; }
        public AsyncRelayCommand LoadCommandAsync { get; set; }

        private readonly FileTransferDbContext _fileTransferDbContext;
        public SendFilePageViewModel(FileTransferDbContext fileTransferDbContext)
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
                        }
                    });
                });

            FileSendViewModels = new ObservableCollection<FileSendViewModel>();
            LoadCommandAsync = new AsyncRelayCommand(LoadAsync);
            _fileTransferDbContext = fileTransferDbContext;
        }

        private bool _loaded = false;
        private async Task LoadAsync()
        {
            if (_loaded) return;
            FileSendViewModels.Clear();
            var records = await _fileTransferDbContext.FileSendRecords
                .Join(_fileTransferDbContext.RemoteChannels, x => x.RemoteId,
                x => x.Id, (x, y) =>
                new FileSendWithRemoteChannelModel
                {
                    FileSendRecordModel = x,
                    RemoteChannelModel = y
                }).Where(x => x.FileSendRecordModel.Status != FileSendStatus.Completed
                && x.FileSendRecordModel.Status != FileSendStatus.Faild).OrderByDescending(x => x.FileSendRecordModel.CreateTime).ToListAsync();

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

            WeakReferenceMessenger.Default.Send(new Tuple<string, int>(null, FileSendViewModels.Count), "TransferSendCount");
        }

        public void RemoveRecordViewModel(FileSendViewModel fileSendViewModel)
        {
            FileSendViewModels.Remove(fileSendViewModel);
            WeakReferenceMessenger.Default.Send(new Tuple<string, int>(null, FileSendViewModels.Count), "TransferSendCount");
        }
    }
}
