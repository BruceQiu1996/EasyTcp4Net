using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using CommunityToolkit.Mvvm.Messaging;
using System.Collections;
using System.Collections.ObjectModel;
using System.Net;
using System.Net.NetworkInformation;
using System.Windows;

namespace EasyTcp4Net.WpfTest
{
    public class MainWindowViewModel : ObservableObject
    {
        private int index = 1;
        private ushort _serverPort => ushort.Parse(_portText);

        private string _portText;
        public string PortText
        {
            get => _portText;
            set => SetProperty(ref _portText, value);
        }

        private string _messages;
        public string Messages
        {
            get => _messages;
            set => SetProperty(ref _messages, value);
        }

        private bool _serverListening;
        public bool ServerListening
        {
            get => _serverListening;
            set => SetProperty(ref _serverListening, value);
        }

        public ObservableCollection<Client> Clients { get; set; } = new ObservableCollection<Client>();

        private Client _selectedClient;
        public Client SelectedClient
        {
            get => _selectedClient;
            set => SetProperty(ref _selectedClient, value);
        }

        private EasyTcpServer _server;
        public RelayCommand LoadCommand { get; set; }
        public RelayCommand StartServerCommand { get; set; }
        public RelayCommand StopServerCommand { get; set; }
        public RelayCommand AddClientCommand { get; set; }
        public MainWindowViewModel()
        {
            LoadCommand = new RelayCommand(() =>
            {
                PortText = PortFilter.GetFirstAvailablePort().ToString();
                _server = new EasyTcpServer(_serverPort);

                _server.OnReceivedData +=  async (obj, e) =>
                {
                    Messages += $"服务端收到:来自{e.Session.RemoteEndPoint.ToString()}:[{System.Text.Encoding.Default.GetString(e.Data.ToArray())}]\n";
                    await _server.SendAsync(e.Session, System.Text.Encoding.Default.GetBytes($"服务器表示收到了{e.Session.RemoteEndPoint.ToString()}的数据"));
                };
            });

            WeakReferenceMessenger.Default
                    .Register<string, string>(this, "AddMessage", async (x, y) =>
                    {
                        Messages += y;
                    });

            StartServerCommand = new RelayCommand(() =>
            {
                _server!.StartListen();
                ServerListening = true;
            });

            StopServerCommand = new RelayCommand(async () =>
            {
                await _server!.CloseAsync();
                ServerListening = false;
            });

            AddClientCommand = new RelayCommand(() =>
            {
                var newClient = new Client($"客户端{index++}", new EasyTcpClient("127.0.0.1", _serverPort));
                Clients.Add(newClient);
                SelectedClient = newClient;
            });
        }
    }

    public class Client : ObservableObject
    {
        private string _sendMessage;
        public string SendMessage
        {
            get => _sendMessage;
            set => SetProperty(ref _sendMessage, value);
        }

        private bool _connected;
        public bool Connected
        {
            get => _connected;
            set => SetProperty(ref _connected, value);
        }

        public string ClientId { get; set; }
        public EasyTcpClient EasyTcpClient { get; set; }

        public AsyncRelayCommand ConnectCommandAsync { get; set; }
        public AsyncRelayCommand DisConnectedCommandAsync { get; set; }
        public AsyncRelayCommand SendAsync { get; set; }
        public Client(string clientid, EasyTcpClient easyTcpClient)
        {
            EasyTcpClient = easyTcpClient;
            ClientId = clientid;
            ConnectCommandAsync = new AsyncRelayCommand(async () =>
            {
                try
                {
                    await EasyTcpClient.ConnectAsync();
                    Connected = true;
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"连接失败:{ex}");
                }
            });
            DisConnectedCommandAsync = new AsyncRelayCommand(async () =>
            {
                await EasyTcpClient.DisConnectAsync();
            });

            easyTcpClient.OnDisConnected += (obj, e) =>
            {
                Connected = false;
            };
            easyTcpClient.OnReceivedData += (obj, e) =>
            {
                WeakReferenceMessenger.Default
                    .Send<string, string>($"{ClientId}:收到[{System.Text.Encoding.Default.GetString(e.Data.ToArray())}]\n", "AddMessage");
            };

            SendAsync = new AsyncRelayCommand(async () =>
            {
                if (!Connected)
                    MessageBox.Show("连接断开");

                if (string.IsNullOrEmpty(SendMessage?.Trim()))
                    return;

                await EasyTcpClient.SendAsync(System.Text.Encoding.Default.GetBytes(SendMessage.Trim()));

                SendMessage = null;
            });
        }
    }

    public class PortFilter
    {
        public static int GetFirstAvailablePort()
        {
            int MAX_PORT = 65535;
            int BEGIN_PORT = 50000;

            for (int i = BEGIN_PORT; i < MAX_PORT; i++)
            {

                if (PortIsAvailable(i)) return i;
            }

            return -1;
        }

        private static IList PortIsUsed()
        {
            IPGlobalProperties ipGlobalProperties = IPGlobalProperties.GetIPGlobalProperties();
            IPEndPoint[] ipsTCP = ipGlobalProperties.GetActiveTcpListeners();
            IPEndPoint[] ipsUDP = ipGlobalProperties.GetActiveUdpListeners();
            TcpConnectionInformation[] tcpConnInfoArray = ipGlobalProperties.GetActiveTcpConnections();
            IList allPorts = new ArrayList();
            foreach (IPEndPoint ep in ipsTCP) allPorts.Add(ep.Port);
            foreach (IPEndPoint ep in ipsUDP) allPorts.Add(ep.Port);
            foreach (TcpConnectionInformation conn in tcpConnInfoArray) allPorts.Add(conn.LocalEndPoint.Port);
            return allPorts;
        }

        private static bool PortIsAvailable(int port)
        {
            bool isAvailable = true;
            IList portUsed = PortIsUsed();
            foreach (int p in portUsed)
            {
                if (p == port)
                {
                    isAvailable = false; break;
                }
            }
            return isAvailable;
        }
    }
}
