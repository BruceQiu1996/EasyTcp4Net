using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using CommunityToolkit.Mvvm.Messaging;
using EasyTcp4Net;
using FileTransfer.Common.Dtos;
using FileTransfer.Common.Dtos.Messages;
using FileTransfer.Common.Dtos.Messages.Connection;
using FileTransfer.Common.Dtos.Transfer;
using FileTransfer.Helpers;
using FileTransfer.Models;
using FileTransfer.Resources;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using System.Buffers.Binary;
using System.IO;
using System.Windows;

namespace FileTransfer.ViewModels
{
    /// <summary>
    /// 连接远端的连接对象
    /// </summary>
    public class RemoteChannelViewModel : ObservableObject
    {
        public string Id { get; set; }
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

        public string Token { get; private set; }
        private readonly EasyTcpClient _easyTcpClient;

        public RemoteChannelViewModel(string id, string ip, ushort port, string remark = null)
        {
            Id = id;
            IPAddress = ip;
            Port = port;
            Remark = remark;
            _easyTcpClient = new EasyTcpClient(ip, port, new EasyTcpClientOptions()
            {
                ConnectRetryTimes = 3
            });
            _easyTcpClient.SetReceiveFilter(new FixedHeaderPackageFilter(16, 8, 4, false));
            _easyTcpClient.OnDisConnected += (obj, e) =>
            {
                Status = "连接断开";
                Connected = false;
            };
            _easyTcpClient.OnReceivedData += async (obj, e) =>
            {
                await HandleMessageAsync(e.Data);
            };
            ConnectCommandAsync = new AsyncRelayCommand(ConnectAsync);
            DropFilesCommandAsync = new AsyncRelayCommand<DragEventArgs>(DropFilesAsync);
        }

        public AsyncRelayCommand ConnectCommandAsync { get; set; }
        public AsyncRelayCommand<DragEventArgs> DropFilesCommandAsync { get; set; }
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
                status = "连接中";
                await _easyTcpClient.ConnectAsync();
                Connected = true;
            }
            catch
            {
                Connected = false;
                Status = "连接失败";
            }
        }

        /// <summary>
        /// 拖拽发送文件
        /// </summary>
        /// <returns></returns>
        private async Task DropFilesAsync(DragEventArgs e)
        {
            var files = (Array)e.Data.GetData(DataFormats.FileDrop);
            foreach (var file in files)
            {
                if (File.Exists(file.ToString()))
                {
                    await SendFileAsync(file.ToString()!);
                }
            }
        }

        /// <summary>
        /// 客户端处理服务端来的信息
        /// </summary>
        /// <returns></returns>
        private async Task HandleMessageAsync(ReadOnlyMemory<byte> data)
        {
            var type = (MessageType)BinaryPrimitives.ReadInt32BigEndian(data.Slice(12, 4).Span);
            switch (type)
            {
                case MessageType.ConnectionAck:
                    {
                        var packet = Packet<ConnectionAck>.FromBytes(data);
                        Token = packet.Body!.SessionToken;
                        Status = "连接成功";
                    }
                    break;
                case MessageType.ApplyTrasnferAck:
                    {
                        var packet = Packet<ApplyFileTransferAck>.FromBytes(data);
                        if (!packet.Body.Approve)
                        {
                            App.ServiceProvider.GetRequiredService<GrowlHelper>()
                                .WarningGlobal("发送失败:" + packet.Body.Message);

                            var _ = Task.Run(async () =>
                            {
                                await Task.Delay(2000);
                                //任务设置成失败
                                //TODO界面上任务取消并且移除到完成界面
                                //数据库更新
                                await App.ServiceProvider.GetRequiredService<DBHelper>()
                                    .UpdateFileSendRecordCompleteAsync(packet.Body.FileSendId, Id, false, packet.Body.Message);
                            });

                        }
                        else
                        {
                            //开始某个发送任务
                            //发送消息到发送文件界面
                            //1.发送的client
                            //2.发送任务ID
                            //3.发送的transferToken
                            WeakReferenceMessenger.Default.Send(new Tuple<EasyTcpClient, string, string>(_easyTcpClient, packet.Body.FileSendId, packet.Body.Token), "StartFileSend");
                        }
                    }
                    break;
            }
        }

        /// <summary>
        /// 发送新文件
        /// </summary>
        /// <param name="file">文件路径</param>
        /// <returns></returns>
        private async Task SendFileAsync(string file)
        {
            if (!connected || string.IsNullOrEmpty(Token))
            {
                HandyControl.Controls.MessageBox
                    .Show("连接断开", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }

            if (!File.Exists(file))
            {
                HandyControl.Controls.MessageBox
                    .Show("文件不存在", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }

            //创建请求到对端
            try
            {
                FileInfo fileInfo = new FileInfo(file);
                string code = null; //sha265
                using (var fileStream = new FileStream(file, FileMode.Open, FileAccess.Read))
                {
                    code = App.ServiceProvider!.GetRequiredService<FileHelper>().ToSHA256(fileStream);

                }
                //创建到数据库，并且添加到传输列表
                var record = new FileSendRecordModel(file, fileInfo.Name, code, fileInfo.Length, Id);
                var result = await App.ServiceProvider!.GetRequiredService<DBHelper>().AddFileSendRecordAsync(record);
                if (!result)
                    throw new Exception("写入到数据库错误");

                //发送到界面
                var channel = await App.ServiceProvider!.GetRequiredService<FileTransferDbContext>()
                    .RemoteChannels.FirstOrDefaultAsync(x => x.Id == Id);

                //根据channel和发送记录生成发送任务viewmodel
                var fileSendViewModel = FileSendViewModel.FromModel(record, channel);
                WeakReferenceMessenger.Default.Send(fileSendViewModel, "AddSendFileRecord");
                await _easyTcpClient.SendAsync(new Packet<ApplyFileTransfer>()
                {
                    MessageType = MessageType.ApplyTrasnfer,
                    Body = new ApplyFileTransfer(fileInfo.Name, record.Id, fileInfo.Length, 0, code, Token)
                }.Serialize());
            }
            catch (Exception ex)
            {
                HandyControl.Controls.MessageBox
                    .Show("文件异常,发送失败", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        /// <summary>
        /// 从模型映射到VM
        /// </summary>
        /// <param name="channelModel">数据模型</param>
        /// <returns></returns>
        public static RemoteChannelViewModel FromModel(RemoteChannelModel channelModel)
        {
            return new RemoteChannelViewModel(channelModel.Id, channelModel.IPAddress, channelModel.Port, channelModel.Remark);
        }
    }
}
