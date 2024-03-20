using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using CommunityToolkit.Mvvm.Messaging;
using System.Collections.ObjectModel;
using System.IO;
using System.Windows;

namespace EasyTcp4Net.WpfTest
{
    /// <summary>
    /// 固定数据头测试
    /// </summary>
    public class MainWindowViewModelFixHeader : ObservableObject
    {
        public static bool IsLittleEndian = false;//默认测试大端在前
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

        public ObservableCollection<ClientFixHeader> Clients { get; set; } = new ObservableCollection<ClientFixHeader>();

        private ClientFixHeader _selectedClient;
        public ClientFixHeader SelectedClient
        {
            get => _selectedClient;
            set => SetProperty(ref _selectedClient, value);
        }

        private EasyTcpServer _server;
        public RelayCommand LoadCommand { get; set; }
        public RelayCommand StartServerCommand { get; set; }
        public RelayCommand StopServerCommand { get; set; }
        public RelayCommand AddClientCommand { get; set; }
        public MainWindowViewModelFixHeader()
        {
            LoadCommand = new RelayCommand(() =>
            {
                PortText = PortFilter.GetFirstAvailablePort().ToString();
                _server = new EasyTcpServer(_serverPort);
                _server.SetReceiveFilter(new FixedHeaderPackageFilter(8 + 4, 8, 4, false));
                _server.OnReceivedData += async (obj, e) =>
                {
                    var packet = new Pakcet<string>();
                    packet.Deserialize(e.Data.Slice(8 + 4).ToArray());
                    Messages += $"服务端收到:来自{e.Session.RemoteEndPoint.ToString()}:[{packet.Body}]\n";
                    await _server.SendAsync(e.Session, new Pakcet<string>() { Body = $"服务器表示收到了{e.Session.RemoteEndPoint.ToString()}的数据" }.Serialize());
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
                var newClient = new ClientFixHeader($"客户端{index++}", new EasyTcpClient("127.0.0.1", _serverPort));
                Clients.Add(newClient);
                SelectedClient = newClient;
            });
        }
    }

    public class ClientFixHeader : ObservableObject
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
        public ClientFixHeader(string clientid, EasyTcpClient easyTcpClient)
        {
            EasyTcpClient = easyTcpClient;
            EasyTcpClient.SetReceiveFilter(new FixedHeaderPackageFilter(8 + 4, 8, 4, false));
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
                var packet = new Pakcet<string>();
                packet.Deserialize(e.Data.Slice(8 +4).ToArray());
                WeakReferenceMessenger.Default
                    .Send<string, string>($"{ClientId}:收到[{packet.Body}]\n", "AddMessage");
            };

            SendAsync = new AsyncRelayCommand(async () =>
            {
                if (!Connected)
                    MessageBox.Show("连接断开");

                if (string.IsNullOrEmpty(SendMessage?.Trim()))
                    return;

                await EasyTcpClient.SendAsync(new Pakcet<string>() 
                {
                    Body = SendMessage,
                }.Serialize());

                SendMessage = null;
            });
        }
    }

    /// <summary>
    /// 数据包
    /// </summary>
    public class Pakcet<TBody>
    {
        //头 8 + 4
        public long Sequence { get; set; }
        public int BodyLength { get; set; }
        //
        public TBody? Body { get; set; }
        public void Deserialize(byte[] bodyData)
        {
            Body = ProtoBufSerializer.DeSerialize<TBody>(bodyData);
        }

        public byte[] Serialize()
        {
            var bodyArray = ProtoBufSerializer.Serialize(Body);
            BodyLength = bodyArray.Length;
            byte[] result = new byte[BodyLength + 8 + 4];
            AddInt64(result, 0, Sequence);
            AddInt32(result, 8, BodyLength);
            Buffer.BlockCopy(bodyArray, 0, result, 8 + 4, bodyArray.Length);

            return result;
        }

        //大端模式添加数据
        public void AddInt32(byte[] buffer, int startIndex, int v)
        {
            buffer[startIndex++] = (byte)(v >> 24);
            buffer[startIndex++] = (byte)(v >> 16);
            buffer[startIndex++] = (byte)(v >> 8);
            buffer[startIndex++] = (byte)v;
        }

        //大端模式添加数据
        public static void AddInt64(byte[] buffer, int startIndex, long v)
        {
            buffer[startIndex++] = (byte)(v >> 56);
            buffer[startIndex++] = (byte)(v >> 48);
            buffer[startIndex++] = (byte)(v >> 40);
            buffer[startIndex++] = (byte)(v >> 32);
            buffer[startIndex++] = (byte)(v >> 24);
            buffer[startIndex++] = (byte)(v >> 16);
            buffer[startIndex++] = (byte)(v >> 8);
            buffer[startIndex++] = (byte)v;
        }
    }

    public class ProtoBufSerializer
    {
        public static byte[] Serialize<T>(T serializeObj)
        {
            try
            {
                using (var stream = new MemoryStream())
                {
                    ProtoBuf.Serializer.Serialize<T>(stream, serializeObj);
                    var result = new byte[stream.Length];
                    stream.Position = 0L;
                    stream.Read(result, 0, result.Length);
                    return result;
                }
            }
            catch (Exception ex)
            {
                return null;
            }
        }

        public static T DeSerialize<T>(byte[] bytes)
        {
            try
            {
                using (var stream = new MemoryStream())
                {
                    stream.Write(bytes, 0, bytes.Length);
                    stream.Position = 0L;
                    return ProtoBuf.Serializer.Deserialize<T>(stream);
                }
            }
            catch
            {
                return default(T);
            }
        }
    }
}
