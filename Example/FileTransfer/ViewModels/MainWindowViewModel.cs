using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using CommunityToolkit.Mvvm.Messaging;
using FileTransfer.Pages;
using System.Windows.Controls;

namespace FileTransfer.ViewModels
{
    public class MainWindowViewModel : ObservableObject
    {
        private Page _currentPage;
        public Page CurrentPage
        {
            get => _currentPage;
            set => SetProperty(ref _currentPage, value);
        }

        private string transferListText = "传输列表";
        public string TransferListText
        {
            get => transferListText;
            set
            {
                SetProperty(ref transferListText, value);
            }
        }

        public RelayCommand OpenTransferPageCommand { get; set; }
        public RelayCommand OpenMainPageCommand { get; set; }
        public MainWindowViewModel(MainPage mainPage, TransferPage transferPage)
        {
            CurrentPage = mainPage;
            OpenTransferPageCommand = new RelayCommand(() =>
            {
                CurrentPage = transferPage;
            });
            OpenMainPageCommand = new RelayCommand(() =>
            {
                CurrentPage = mainPage;
            });
            WeakReferenceMessenger.Default.Register<MainWindowViewModel,
              Tuple<string, int>, string>(this, "TasksCount", async (x, y) =>
              {
                  TransferListText = y.Item2 == 0 ? "传输列表" : $"传输列表[{y.Item2}]";
              });
        }
    }
}
