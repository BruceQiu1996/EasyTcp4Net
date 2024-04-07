using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using EasyTcp4Net;
using FileSenderCommon.Dtos;
using System.Buffers.Binary;
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
            _client = new EasyTcpClient("127.0.0.1", 7007, new EasyTcpClientOptions()
            {
                ConnectRetryTimes = 1,
                ConnectTimeout = 3000
            });
            _client.SetReceiveFilter(new FixedHeaderPackageFilter(16, 8, 4, false));
            _client.OnDisConnected += (obj, e) =>
            {
                Connected = false;
            };

            _client.OnReceivedData += (obj, e) =>
            {
                var messageTypeBytes = e.Data.Slice(12, 4);
                var body = e.Data.Slice(16);
                var type = BinaryPrimitives.ReadInt32BigEndian(messageTypeBytes.Span);

                switch ((MessageType)type) 
                {
                    case MessageType.MembersPush:
                        HandleMembersPush(new BasePacket<MembersPushMessage>().Deserialize(body.ToArray()));
                        break;
                }
            };
            LoadCommandAsync = new AsyncRelayCommand(LoadAsync);
        }

        private void HandleMembersPush(BasePacket<MembersPushMessage> packet) 
        {
            
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
                MessageBox.Show(ex.ToString(), "错误", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
    }
}
