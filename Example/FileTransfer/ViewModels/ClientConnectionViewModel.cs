using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Messaging;
using EasyTcp4Net;
using FileTransfer.Common.Dtos;
using FileTransfer.Common.Dtos.Messages;
using FileTransfer.Common.Dtos.Transfer;
using FileTransfer.Helpers;
using FileTransfer.Models;
using Microsoft.Extensions.DependencyInjection;
using System.Buffers.Binary;
using System.IO;
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

                        if (string.IsNullOrEmpty(packet.Body.FileName) || string.IsNullOrEmpty(packet.Body.Code)
                            || string.IsNullOrEmpty(packet.Body.FileSendId))
                        {
                            await Session.SendAsync(new Packet<ApplyFileTransferAck>()
                            {
                                MessageType = MessageType.ApplyTrasnferAck,
                                Body = new ApplyFileTransferAck()
                                {
                                    Approve = false,
                                    FileSendId = packet.Body.FileSendId,
                                    Message = "传输文件数据异常"
                                }
                            }.Serialize());

                            return;
                        }

                        var fileHelper = App.ServiceProvider.GetRequiredService<FileHelper>();
                        var iniSettings = App.ServiceProvider.GetRequiredService<IniSettings>();
                        var canSave = fileHelper.PathCanSave(iniSettings.FileSaveLocation, packet.Body.TotalSize);
                        if (!canSave)
                        {
                            await Session.SendAsync(new Packet<ApplyFileTransferAck>()
                            {
                                MessageType = MessageType.ApplyTrasnferAck,
                                Body = new ApplyFileTransferAck()
                                {
                                    FileSendId = packet.Body.FileSendId,
                                    Approve = false,
                                    Message = "目标机器存储空间异常"
                                }
                            }.Serialize());

                            return;
                        }

                        if (packet.Body!.SessionToken != SessionToken)
                        {
                            await Session.SendAsync(new Packet<ApplyFileTransferAck>()
                            {
                                MessageType = MessageType.ApplyTrasnferAck,
                                Body = new ApplyFileTransferAck()
                                {
                                    Approve = false,
                                    FileSendId = packet.Body.FileSendId,
                                    Message = "会话密钥错误"
                                }
                            }.Serialize());

                            return;
                        }

                        if (packet.Body.StartIndex == 0 && string.IsNullOrEmpty(packet.Body.TransferToken)) //新发送的文件TODO 根据fileSendid，sha256判断是新的传输还是断点续传,以及是否允许断点续传
                        {
                            var allow =
                                App.ServiceProvider!.GetRequiredService<IniSettings>().AgreeTransfer;
                            if (!allow)
                            {
                                var agree = true;
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
                                                FileSendId = packet.Body.FileSendId,
                                                Message = "对方未同意"
                                            }
                                        }.Serialize());

                                        agree = false;

                                        return;
                                    }

                                    agree = true;
                                });

                                if (!agree) return;
                            }
                            //创建任务
                            //创建一个临时文件
                            var tempLocation = Path.Combine(iniSettings.FileSaveLocation, $"{Path.GetRandomFileName()}.tmp");
                            File.Create(tempLocation).Close();
                            var task = new FileReceiveRecordModel(packet.Body.FileName, packet.Body.Code, packet.Body.TotalSize, tempLocation,
                                packet.Body.FileSendId, RemoteEndPoint);
                            await App.ServiceProvider.GetRequiredService<DBHelper>().AddFileReceiveRecordAsync(task);
                            var transferToken = Guid.NewGuid().ToString();
                            var fileReceiveViewModel = FileReceiveViewModel.FromModel(task); //通过数据库接收文件模型创建接收文件视图模型
                            WeakReferenceMessenger.Default.Send(fileReceiveViewModel, "AddReceiveFileRecord");
                            await Session.SendAsync(new Packet<ApplyFileTransferAck>()
                            {
                                MessageType = MessageType.ApplyTrasnferAck,
                                Body = new ApplyFileTransferAck()
                                {
                                    Approve = true,
                                    FileSendId = packet.Body.FileSendId,
                                    Token = fileReceiveViewModel.TransferToken
                                }
                            }.Serialize());
                        }
                    }
                    break;
                case MessageType.FileSend:
                    {
                        var packet = Packet<FileSegement>.FromBytes(data);
                        WeakReferenceMessenger.Default.Send(packet.Body!, "ReceiveFileData");
                    }
                    break;
            }
        }
    }
}
