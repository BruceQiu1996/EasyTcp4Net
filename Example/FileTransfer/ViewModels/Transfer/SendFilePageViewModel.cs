using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using CommunityToolkit.Mvvm.Messaging;
using FileTransfer.Models;
using FileTransfer.Resources;
using Microsoft.EntityFrameworkCore;
using System.Collections.ObjectModel;
using System.Windows;

namespace FileTransfer.ViewModels.Transfer
{
    public class SendFilePageViewModel :  ObservableObject
    {
        private ObservableCollection<FileSendViewModel> fileSendViewModels = new ObservableCollection<FileSendViewModel>();
        public ObservableCollection<FileSendViewModel> FileSendViewModels
        {
            get => fileSendViewModels;
            set => SetProperty(ref fileSendViewModels, value);
        }

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
                        FileSendViewModels.Add(y);
                    });
                });

            LoadCommandAsync = new AsyncRelayCommand(LoadAsync);
            _fileTransferDbContext = fileTransferDbContext;
        }

        private async Task LoadAsync() 
        {
            FileSendViewModels.Clear();
            var records = await _fileTransferDbContext.FileSendRecords.Join(_fileTransferDbContext.RemoteChannels, x => x.RemoteId,
                x => x.Id, (x, y) =>
                new FileSendWithRemoteChannelModel
                {
                    FileSendRecordModel = x,
                    RemoteChannelModel = y
                }).Where(x => x.FileSendRecordModel.Status != FileSendStatus.Completed 
                && x.FileSendRecordModel.Status != FileSendStatus.Faild).ToListAsync();

            records.ForEach(x =>
            {
                FileSendViewModels.Add(FileSendViewModel.FromModel(x.FileSendRecordModel,x.RemoteChannelModel));
            });
        }
    }
}
