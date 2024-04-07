using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using EasyTcp4Net;
using System.Windows;

namespace FileSenderClient
{
    public class MainWindowViewModel : ObservableObject
    {
        private bool connected;
        public bool Connected 
        { 
            get => connected; 
            set => SetProperty(ref connected, value);
        }

        public AsyncRelayCommand LoadCommandAsync { get; set; }

        private readonly EasyTcpClient _client;
        public MainWindowViewModel()
        {
            _client = new EasyTcpClient("127.0.0.1", 7007,new EasyTcpClientOptions() 
            {
                ConnectRetryTimes = 1,
                ConnectTimeout = 3000
            });

            _client.OnDisConnected += (obj, e) =>
            {
                Connected = false;
            };
            LoadCommandAsync = new AsyncRelayCommand(LoadAsync);
        }

        private async Task LoadAsync()
        {
            try
            {
                await _client.ConnectAsync();
                Connected = true;
            }
            catch (Exception ex) 
            {
                MessageBox.Show(ex.ToString(),"错误",MessageBoxButton.OK,MessageBoxImage.Error);
            }
        }
    }
}
