using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using CommunityToolkit.Mvvm.Messaging;
using FileTransfer.Common.Dtos.Transfer;
using FileTransfer.Models;
using FileTransfer.Resources;
using Microsoft.EntityFrameworkCore;
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

            WeakReferenceMessenger.Default.Register<ReceiveFilePageViewModel,
                string, string>(this, "Load", async (x, y) =>
                {
                    await LoadAsync();
                });

            WeakReferenceMessenger.Default.Register<ReceiveFilePageViewModel,
               FileSegement, string>(this, "ReceiveFileData", async (x, y) =>
               {
                   var viewModel = FileReceiveViewModels.FirstOrDefault(x => x.FileSendId == y.FileSendId);
                   if (viewModel != null && viewModel.TransferToken == y.TransferToken)
                   {
                       await viewModel.ReceiveDataAsync(y);
                   }
               });

            WeakReferenceMessenger.Default.Register<ReceiveFilePageViewModel,
               string, string>(this, "ReceiveFinish", async (x, y) =>
               {
                   Application.Current.Dispatcher.Invoke(() =>
                   {
                       var viewModel = FileReceiveViewModels.FirstOrDefault(x => x.Id == y);
                       if (viewModel != null)
                       {
                           FileReceiveViewModels.Remove(viewModel);
                       }
                   });
               });

            LoadCommandAsync = new AsyncRelayCommand(LoadAsync);
            _fileTransferDbContext = fileTransferDbContext;
        }

        private bool _loaded = false;
        private async Task LoadAsync()
        {
            if (_loaded) return;
            FileReceiveViewModels.Clear();
            var records = await _fileTransferDbContext.FileReceiveRecords.Where(x =>
            x.Status != FileReceiveStatus.Faild && x.Status != FileReceiveStatus.Completed).ToListAsync();

            records.ForEach(x =>
            {
                FileReceiveViewModels.Add(FileReceiveViewModel.FromModel(x));
            });
            _loaded = true;
        }
    }
}
