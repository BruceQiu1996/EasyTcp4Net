using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
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
        }
    }
}
