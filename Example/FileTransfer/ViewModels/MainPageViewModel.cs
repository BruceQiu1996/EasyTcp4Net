using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using CommunityToolkit.Mvvm.Messaging;
using EasyTcp4Net;
using FileTransfer.Common.Dtos;
using FileTransfer.Common.Dtos.Messages.Connection;
using FileTransfer.Helpers;
using FileTransfer.Models;
using FileTransfer.Resources;
using FileTransfer.ViewModels.Transfer;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Win32;
using System.Collections.Concurrent;
using System.Collections.ObjectModel;
using System.IO;
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

        private string fileSaveLocation;
        public string FileSaveLocation
        {
            get => fileSaveLocation;
            set => SetProperty(ref fileSaveLocation, value);
        }

        private ObservableCollection<RemoteChannelViewModel> remoteChannelViewModels = new ObservableCollection<RemoteChannelViewModel>();
        public ObservableCollection<RemoteChannelViewModel> RemoteChannelViewModels
        {
            get => remoteChannelViewModels;
            set => SetProperty(ref remoteChannelViewModels, value);
        }

        private readonly ConcurrentDictionary<string, ClientConnectedViewModel> _clients = new ConcurrentDictionary<string, ClientConnectedViewModel>();
        private readonly IniSettings _settings;
        private readonly NetHelper _netHelper;
        private readonly DBHelper _dBHelper;
        private readonly GrowlHelper _grolHelper;
        private EasyTcpServer _easyTcpServer;
        public MainPageViewModel(IniSettings iniSettings, NetHelper netHelper,
            DBHelper dBHelper, GrowlHelper grolHelper)
        {
            _settings = iniSettings;
            _netHelper = netHelper;
            _dBHelper = dBHelper;
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
            SendFileCommandAsync = new AsyncRelayCommand(SendFileAsync);
            ChooseSaveFileLocationCommandAsync = new AsyncRelayCommand(ChooseSaveFileLocationAsync);
            _grolHelper = grolHelper;
        }

        public AsyncRelayCommand LoadCommandAsync { get; set; }
        public AsyncRelayCommand StartListeningCommandAsync { get; set; }
        public AsyncRelayCommand StopListeningCommandAsync { get; set; }
        public RelayCommand AddRemoteChannelCommand { get; set; }
        public AsyncRelayCommand SendFileCommandAsync { get; set; }
        public AsyncRelayCommand ChooseSaveFileLocationCommandAsync { get; set; }

        private bool _loaded = false;
        private async Task LoadAsync()
        {
            if (_loaded)
                return;
            if (_settings.Port != 0)
            {
                Port = _settings.Port.ToString();
            }
            else
            {
                Port = _netHelper.GetFirstAvailablePort().ToString();
                await _settings.WritePortAsync(ushort.Parse(Port));
            }

            var remoteChannels = await _dBHelper.GetAllAsync<RemoteChannelModel>();
            foreach (var item in remoteChannels.OrderByDescending(x => x.CreateTime))
            {
                RemoteChannelViewModels.Add(RemoteChannelViewModel.FromModel(item));
            }

            AgreeConnect = _settings.AgreeConnect;
            AgreeTransfer = _settings.AgreeTransfer;
            //加载存储位置
            if (string.IsNullOrEmpty(_settings.FileSaveLocation))
            {
                var path = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments), "EasyFileTransfer");
                if (!Directory.Exists(path))
                {
                    Directory.CreateDirectory(path);
                }

                await _settings.WriteFileSaveLocationAsync(path);
            }
            FileSaveLocation = _settings.FileSaveLocation;
            if (!Directory.Exists(FileSaveLocation))
            {
                _grolHelper.Warning("文件存储目录不存在");
            }
            //加载发送和接收页
            WeakReferenceMessenger.Default.Send(string.Empty, "Load");
            _loaded = true;

            ///自动监听
            await StartListeningAsync();
        }

        /// <summary>
        /// 选择存放文件的位置
        /// </summary>
        private async Task ChooseSaveFileLocationAsync()
        {
            OpenFolderDialog openFolderDialog = new OpenFolderDialog();
            var result = openFolderDialog.ShowDialog();
            if (result != null && result.Value)
            {
                await _settings.WriteFileSaveLocationAsync(openFolderDialog.FolderName);
                FileSaveLocation = _settings.FileSaveLocation;
            }
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
                    _easyTcpServer.OnReceivedData -= OnReceiveDataAsync!;
                }
                _easyTcpServer = new EasyTcpServer(_settings.Port, new EasyTcpServerOptions()
                {
                    BufferSize = 8 * 1024,
                    MaxPipeBufferSize = int.MaxValue
                });
                _easyTcpServer.OnClientConnectionChanged += OnNewClientConnectedAsync!;
                _easyTcpServer.OnReceivedData += OnReceiveDataAsync!;
                _easyTcpServer.SetReceiveFilter(new FixedHeaderPackageFilter(16, 8, 4, false));
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
            if (e.Status == ConnectsionStatus.DisConnected)
            {
                _clients.TryRemove(e.ClientSession.SessionId, out var temp);
                await _easyTcpServer.DisconnectClientAsync(e.ClientSession);
                //TODO如果有该对象发送过来的文件数据则需要处理

                return;
            }
            if (e.Status == ConnectsionStatus.Connected && _clients.ContainsKey(e.ClientSession.SessionId))
            {
                await _easyTcpServer.DisconnectClientAsync(e.ClientSession);
                return;
            }

            //if (e.Status == ConnectsionStatus.Connected && !_settings.AgreeConnect) //不经过允许的连接
            //{
            //    await Application.Current.Dispatcher.InvokeAsync(async () =>
            //    {
            //        AgreeConnectWindow agreeConnectWindow = new AgreeConnectWindow(e.ClientSession.RemoteEndPoint.ToString());
            //        agreeConnectWindow.Topmost = true;
            //        agreeConnectWindow.WindowStartupLocation = WindowStartupLocation.CenterScreen;
            //        var result = agreeConnectWindow.ShowDialog();
            //        if (result != null && !result.Value)
            //        {
            //            await _easyTcpServer.DisconnectClientAsync(e.ClientSession);
            //            return;
            //        }
            //    });
            //}

            var vm = new ClientConnectedViewModel(e.ClientSession);
            _clients.TryAdd(e.ClientSession.SessionId, vm);
            //发送token到连接客户端
            await e.ClientSession.SendAsync(new Packet<ConnectionAck>()
            {
                MessageType = Common.Dtos.Messages.MessageType.ConnectionAck,
                Body = new ConnectionAck() { }
            }.Serialize());
        }

        /// <summary>
        /// 当服务端后到消息后
        /// </summary>
        /// <param name="obj"></param>
        /// <param name="e"></param>
        private async void OnReceiveDataAsync(object obj, ServerDataReceiveEventArgs e)
        {
            if (_clients.ContainsKey(e.Session.SessionId))
            {
                await _clients[e.Session.SessionId].OnReceiveDataAsync(e.Data);
            }
        }

        private async Task SendFileAsync()
        {

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

            var channel = await _dBHelper
                .FirstOrDefaultAsync<RemoteChannelModel>(x => x.IPAddress == ip && x.Port == port);
            if (channel != null)
            {
                HandyControl.Controls.MessageBox.Show("远程连接已存在", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }

            var model = new RemoteChannelModel()
            {
                Remark = remark,
                IPAddress = ip,
                Port = port
            };
            await _dBHelper.AddRemoteChannelAsync(model);

            RemoteChannelViewModels.Add(new RemoteChannelViewModel(model.Id, ip, port, remark));
            HandyControl.Controls.MessageBox.Show("添加成功", "信息", MessageBoxButton.OK, MessageBoxImage.Information);
        }
    }
}
