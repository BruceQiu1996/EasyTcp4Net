using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Messaging;
using FileTransfer.Models;
using System.Collections.ObjectModel;

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

        public SendFilePageViewModel()
        {
            WeakReferenceMessenger.Default.Register<SendFilePageViewModel,
                FileSendRecordModel, string>(this, "AddSendFileRecord", async (x, y) =>
                {
                    FileSendViewModels.Add(FileSendViewModel.FromModel(y));
                });
        }
    }
}
