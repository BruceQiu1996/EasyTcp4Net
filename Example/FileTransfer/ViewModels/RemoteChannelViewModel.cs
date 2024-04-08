using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using EasyTcp4Net;

namespace FileTransfer.ViewModels
{
    public class RemoteChannelViewModel : ObservableObject
    {
        private string remark;
        public string Remark
        {
            get => remark;
            set => SetProperty(ref remark, value);
        }

        public string IPAddress { get; set; }
        public ushort Port { get; set; }

        private bool connected;
        public bool Connected
        {
            get => connected;
            set => SetProperty(ref connected, value);
        }

        private string status;
        public string Status
        {
            get => status;
            set => SetProperty(ref status, value);
        }

        private readonly EasyTcpClient _easyTcpClient;
        public RemoteChannelViewModel(string ip, ushort port, string remark = null)
        {
            IPAddress = ip;
            Port = port;
            Remark = remark;
            _easyTcpClient = new EasyTcpClient(ip, port,new EasyTcpClientOptions() 
            {
                ConnectRetryTimes = 3
            });

            _easyTcpClient.OnDisConnected += (obj, e) =>
            {
                Status = "连接断开";
                Connected = false;
            };
            ConnectCommandAsync = new AsyncRelayCommand(ConnectAsync);
        }

        public AsyncRelayCommand ConnectCommandAsync { get; set; }
        /// <summary>
        /// 连接远端
        /// </summary>
        /// <returns></returns>
        public async Task ConnectAsync() 
        {
            if (Connected)
                return;
            try
            {
                await _easyTcpClient.ConnectAsync();
                Connected = true;
                Status = "已连接";
            }
            catch
            {
                Connected = false;
                Status = "连接失败";
            }
        }
    }
}
