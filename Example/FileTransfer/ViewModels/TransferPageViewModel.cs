using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using CommunityToolkit.Mvvm.Messaging;
using FileTransfer.Models;
using FileTransfer.Pages.Transfer;
using FileTransfer.ViewModels.Transfer;
using System.Windows.Controls;

namespace FileTransfer.ViewModels
{
    public class TransferPageViewModel : ObservableObject
    {
        private Page _currentPage;
        public Page CurrentPage
        {
            get => _currentPage;
            set => SetProperty(ref _currentPage, value);
        }

        public RelayCommand OpenSendFilePageCommand { get; set; }
        public RelayCommand OpenReceiveFilePageCommand { get; set; }
        public RelayCommand OpenCompleteTransferPageCommand { get; set; }
        public TransferPageViewModel(SendFilePage sendFilePage, ReceiveFilePage receiveFilePage, CompleteTransferPage completeTransferPage)
        {
            OpenSendFilePageCommand = new RelayCommand(() =>
            {
                CurrentPage = sendFilePage;
            });

            OpenReceiveFilePageCommand = new RelayCommand(() =>
            {
                CurrentPage = receiveFilePage;
            });

            OpenCompleteTransferPageCommand = new RelayCommand(() =>
            {
                CurrentPage = completeTransferPage;
            });

            CurrentPage = sendFilePage;

            WeakReferenceMessenger.Default.Register<TransferPageViewModel, string, string>(this, "Load", async (x, y) =>
            {
                
            });
        }
    }
}
