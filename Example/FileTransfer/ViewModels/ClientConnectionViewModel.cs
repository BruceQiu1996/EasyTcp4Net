using CommunityToolkit.Mvvm.ComponentModel;
using EasyTcp4Net;
using FileTransfer.Common.Dtos;
using FileTransfer.Common.Dtos.Messages;
using FileTransfer.Common.Dtos.Transfer;
using FileTransfer.Helpers;
using Microsoft.Extensions.DependencyInjection;
using System.Buffers.Binary;
using System.Windows;

namespace FileTransfer.ViewModels
{
    /// <summary>
    /// 表示已经连接到该应用的客户端连接信息视图模型
    /// </summary>
    public class ClientConnectedViewModel : ObservableObject
    {
        public ClientSession Session { get; set; }
        public string SessionToken { get; private set; } = Guid.NewGuid().ToString();
        public DateTime? LastConnectedTime { get; set; }
        public string RemoteEndPoint => Session.RemoteEndPoint.ToString();

        public ClientConnectedViewModel(ClientSession clientSession)
        {
            Session = clientSession;
        }

        public void RefreshToken()
        {
            SessionToken = Guid.NewGuid().ToString();
        }

        /// <summary>
        /// 服务端收到的数据转到这里
        /// </summary>
        /// <returns></returns>
        public async Task OnReceiveDataAsync(ReadOnlyMemory<byte> data)
        {
            var type = (MessageType)BinaryPrimitives.ReadInt32BigEndian(data.Slice(12, 4).Span);
            switch (type)
            {
                case MessageType.ApplyTrasnfer:
                    {
                        var packet = Packet<ApplyFileTransfer>.FromBytes(data);
                        if (packet.Body!.SessionToken != SessionToken)
                        {
                            await Session.SendAsync(new Packet<ApplyFileTransferAck>()
                            {
                                MessageType = MessageType.ApplyTrasnferAck,
                                Body = new ApplyFileTransferAck()
                                {
                                    Approve = false,
                                    Message = "会话密钥错误"
                                }
                            }.Serialize());

                            return;
                        }

                        if (packet.Body.StartIndex == 0 && string.IsNullOrEmpty(packet.Body.TransferToken)) //新发送的文件
                        {
                            var allow =
                                App.ServiceProvider!.GetRequiredService<IniSettings>().AgreeTransfer;
                            if (!allow)
                            {
                                //弹窗确认
                                await Application.Current.Dispatcher.InvokeAsync(async () =>
                                {
                                    AgreeTransferWindow agreeConnectWindow =
                                        new AgreeTransferWindow(Session.RemoteEndPoint.ToString(), packet.Body.FileName, packet.Body.TotalSize);
                                    agreeConnectWindow.Topmost = true;
                                    agreeConnectWindow.WindowStartupLocation = WindowStartupLocation.CenterScreen;
                                    var result = agreeConnectWindow.ShowDialog();
                                    if (result != null && !result.Value)
                                    {
                                        await Session.SendAsync(new Packet<ApplyFileTransferAck>()
                                        {
                                            MessageType = MessageType.ApplyTrasnferAck,
                                            Body = new ApplyFileTransferAck()
                                            {
                                                Approve = false,
                                                Message = "对方未同意"
                                            }
                                        }.Serialize());

                                        return;
                                    }
                                });
                            }

                            await Session.SendAsync(new Packet<ApplyFileTransferAck>()
                            {
                                MessageType = MessageType.ApplyTrasnferAck,
                                Body = new ApplyFileTransferAck()
                                {
                                    Approve = true,
                                    Token = Guid.NewGuid().ToString()
                                }
                            }.Serialize());
                        }

                        //断点续传
                        if (!string.IsNullOrEmpty(packet.Body.TransferToken))
                        {

                        }
                    }
                    break;
            }
        }
    }
}
