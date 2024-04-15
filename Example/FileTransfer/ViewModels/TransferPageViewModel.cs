using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using CommunityToolkit.Mvvm.Messaging;
using FileTransfer.Pages.Transfer;
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

        private string _sendingCount;
        public string SendingCount
        {
            get => _sendingCount;
            set => SetProperty(ref _sendingCount, value);
        }

        private string _receivingCount;
        public string ReceivingCount
        {
            get => _receivingCount;
            set => SetProperty(ref _receivingCount, value);
        }

        public RelayCommand OpenSendFilePageCommand { get; set; }
        public RelayCommand OpenReceiveFilePageCommand { get; set; }
        public RelayCommand OpenCompleteTransferPageCommand { get; set; }
        private int allCount = 0;
        private int sendCount = 0;
        private int receiveCount = 0;
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

            WeakReferenceMessenger.Default
                .Register<TransferPageViewModel, Tuple<string, int>, string>(this, "TransferReceiveCount", (x, y) =>
            {
                ReceivingCount = y.Item2 == 0 ? null : y.Item2.ToString();
                receiveCount = y.Item2;
                allCount = receiveCount + sendCount;
                WeakReferenceMessenger.Default.Send(new Tuple<string, int>(null, allCount), "TasksCount");
            });

            WeakReferenceMessenger.Default
                .Register<TransferPageViewModel, Tuple<string, int>, string>(this, "TransferSendCount", (x, y) =>
                {
                    SendingCount = y.Item2 == 0 ? null : y.Item2.ToString();
                    sendCount = y.Item2;
                    allCount = receiveCount + sendCount;
                    WeakReferenceMessenger.Default.Send(new Tuple<string, int>(null, allCount), "TasksCount");
                });
        }
    }
}
