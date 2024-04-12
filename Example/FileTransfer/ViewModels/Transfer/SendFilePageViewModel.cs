using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using CommunityToolkit.Mvvm.Messaging;
using EasyTcp4Net;
using FileTransfer.Common.Dtos;
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
                        FileSendViewModels!.Insert(0, y);
                    });
                });

            WeakReferenceMessenger.Default.Register<SendFilePageViewModel,
                string, string>(this, "Load", async (x, y) =>
                {
                    await LoadAsync();
                });

            WeakReferenceMessenger.Default.Register<SendFilePageViewModel,
                Tuple<EasyTcpClient, string, string>, string>(this, "StartFileSend", async (x, y) =>
                {
                    var viewModel = FileSendViewModels!.FirstOrDefault(x => x.Id == y.Item2);
                    if (viewModel != null)
                    {
                        await viewModel.SendAsync(y.Item1, y.Item3); //client,token
                    }
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
            var records = await _fileTransferDbContext.FileSendRecords.Join(_fileTransferDbContext.RemoteChannels, x => x.RemoteId,
                x => x.Id, (x, y) =>
                new FileSendWithRemoteChannelModel
                {
                    FileSendRecordModel = x,
                    RemoteChannelModel = y
                }).Where(x => x.FileSendRecordModel.Status != FileSendStatus.Completed
                && x.FileSendRecordModel.Status != FileSendStatus.Faild).OrderByDescending(x => x.FileSendRecordModel.CreateTime).ToListAsync();

            records.ForEach(x =>
            {
                FileSendViewModels.Add(FileSendViewModel.FromModel(x.FileSendRecordModel, x.RemoteChannelModel));
            });

            _loaded = true;
        }
    }
}
