using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using CommunityToolkit.Mvvm.Messaging;
using EasyTcp4Net;
using FileTransfer.Helpers;
using FileTransfer.Models;
using Microsoft.Extensions.DependencyInjection;
using System.Collections.ObjectModel;
using System.Text.Json;
using System.Windows;

namespace FileTransfer.ViewModels
{
    public class MainPageViewModel : ObservableObject
    {
        private string _port;
        public string Port
        {
            get => _port;
            set => SetProperty(ref _port, value);
        }

        private bool _startListening;
        public bool StartListening
        {
            get => _startListening;
            set => SetProperty(ref _startListening, value);
        }

        private bool agreeConnect;
        public bool AgreeConnect
        {
            get => agreeConnect;
            set
            {
                _settings.WriteAgreeConnectAsync(value).ConfigureAwait(false).GetAwaiter().GetResult();
                SetProperty(ref agreeConnect, value);
            }
        }

        private bool agreeTransfer;
        public bool AgreeTransfer
        {
            get => agreeTransfer;
            set
            {
                _settings.WriteAgreeTransferAsync(value).ConfigureAwait(false).GetAwaiter().GetResult();
                SetProperty(ref agreeTransfer, value);
            }
        }

        private ObservableCollection<RemoteChannelViewModel> remoteChannelViewModels = new ObservableCollection<RemoteChannelViewModel>();
        public ObservableCollection<RemoteChannelViewModel> RemoteChannelViewModels
        {
            get => remoteChannelViewModels;
            set => SetProperty(ref remoteChannelViewModels, value);
        }

        private readonly IniSettings _settings;
        private readonly NetHelper _netHelper;
        private EasyTcpServer _easyTcpServer;
        public MainPageViewModel(IniSettings iniSettings, NetHelper netHelper)
        {
            _settings = iniSettings;
            _netHelper = netHelper;
            WeakReferenceMessenger.Default.Register<MainPageViewModel,
                Tuple<string, string, ushort>, string>(this, "AddRemoteChannel", async (x, y) =>
            {
                await AddRemoteChannelAsync(y.Item1, y.Item2, y.Item3);
            });
            LoadCommandAsync = new AsyncRelayCommand(LoadAsync);
            StartListeningCommandAsync = new AsyncRelayCommand(StartListeningAsync);
            StopListeningCommandAsync = new AsyncRelayCommand(StopListeningAsync);
            AddRemoteChannelCommand = new RelayCommand(() =>
            {
                var window = new AddRemoteChannelWindow();
                window.Owner = App.ServiceProvider.GetRequiredService<MainWindow>();
                window.WindowStartupLocation = WindowStartupLocation.CenterOwner;
                window.ShowDialog();
            });
        }

        public AsyncRelayCommand LoadCommandAsync { get; set; }
        public AsyncRelayCommand StartListeningCommandAsync { get; set; }
        public AsyncRelayCommand StopListeningCommandAsync { get; set; }
        public RelayCommand AddRemoteChannelCommand { get; set; }
        private async Task LoadAsync()
        {
            if (_settings.Port != 0)
            {
                Port = _settings.Port.ToString();
            }
            else
            {
                Port = _netHelper.GetFirstAvailablePort().ToString();
                await _settings.WritePortAsync(ushort.Parse(Port));
            }

            if (!string.IsNullOrEmpty(_settings.Remotes))
            {
                var remotes =
                    JsonSerializer.Deserialize<IEnumerable<RemoteChannelModel>>(_settings.Remotes);

                if (remotes != null)
                {
                    foreach (var item in remotes)
                    {
                        RemoteChannelViewModels.Add(new RemoteChannelViewModel(item.IPAddress, item.Port, item.Remark));
                    }
                }
            }

            AgreeConnect = _settings.AgreeConnect;
            AgreeTransfer = _settings.AgreeTransfer;
        }

        /// <summary>
        /// 开始监听
        /// </summary>
        /// <returns></returns>
        private async Task StartListeningAsync()
        {
            if (StartListening)
                return;

            try
            {
                var value = ushort.TryParse(Port, out var temp) ? temp : 0;
                if (value == 0)
                {
                    HandyControl.Controls.MessageBox.Show("错误的端口号", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
                    return;
                }

                await _settings.WritePortAsync(ushort.Parse(Port));
                if (_easyTcpServer != null)
                {
                    await _easyTcpServer.CloseAsync();
                    _easyTcpServer.OnClientConnectionChanged -= OnNewClientConnectedAsync!;
                }
                _easyTcpServer = new EasyTcpServer(_settings.Port);
                _easyTcpServer.OnClientConnectionChanged += OnNewClientConnectedAsync!;
                _easyTcpServer.StartListen();
                StartListening = true;
            }
            catch (Exception ex)
            {
                HandyControl.Controls.MessageBox.Show(ex.ToString(), "错误", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        /// <summary>
        /// 当有新的客户端连接的时候
        /// </summary>
        /// <param name="obj"></param>
        /// <param name="e"></param>
        private async void OnNewClientConnectedAsync(object obj, ServerSideClientConnectionChangeEventArgs e)
        {
            if (e.Status == ConnectsionStatus.Connected && !_settings.AgreeConnect) //不经过允许的连接
            {
                await Application.Current.Dispatcher.InvokeAsync(async () =>
                {
                    AgreeConnectWindow agreeConnectWindow = new AgreeConnectWindow(e.ClientSession.RemoteEndPoint.ToString());
                    agreeConnectWindow.Topmost = true;
                    agreeConnectWindow.WindowStartupLocation = WindowStartupLocation.CenterScreen;
                    var result = agreeConnectWindow.ShowDialog();
                    if (result != null && !result.Value)
                    {
                        await _easyTcpServer.DisconnectClientAsync(e.ClientSession);
                    }
                });
            }

            //TODO添加到连接队列
        }

        /// <summary>
        /// 停止监听
        /// </summary>
        /// <returns></returns>
        private async Task StopListeningAsync()
        {
            if (!StartListening)
                return;

            await _easyTcpServer?.CloseAsync();
            StartListening = false;
        }

        /// <summary>
        /// 增加远程连接
        /// </summary>
        /// <returns></returns>
        private async Task AddRemoteChannelAsync(string remark, string ip, ushort port)
        {
            if (RemoteChannelViewModels
                .FirstOrDefault(x => x.IPAddress == ip && x.Port == port) != null)
            {
                HandyControl.Controls.MessageBox.Show("远程连接已存在", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }

            List<RemoteChannelModel> remotes = null;
            if (string.IsNullOrEmpty(_settings.Remotes))
            {
                remotes = new List<RemoteChannelModel>();
            }
            else
            {
                remotes = JsonSerializer.Deserialize<List<RemoteChannelModel>>(_settings.Remotes);
                if (remotes == null)
                {
                    remotes = new List<RemoteChannelModel>();
                }
            }

            remotes.Add(new RemoteChannelModel()
            {
                IPAddress = ip,
                Port = port,
                Remark = remark
            });

            await _settings.WriteRemotesAsync(JsonSerializer.Serialize(remotes));
            RemoteChannelViewModels.Add(new RemoteChannelViewModel(ip, port, remark));

            HandyControl.Controls.MessageBox.Show("添加成功", "信息", MessageBoxButton.OK, MessageBoxImage.Information);
        }
    }
}
