using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Messaging;
using EasyTcp4Net;
using FileTransfer.Common.Dtos;
using FileTransfer.Common.Dtos.Messages;
using FileTransfer.Common.Dtos.Transfer;
using FileTransfer.Helpers;
using FileTransfer.Models;
using FileTransfer.ViewModels.Transfer;
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
        public DateTime? LastConnectedTime { get; set; }
        public string RemoteEndPoint => Session.RemoteEndPoint.ToString();

        private FileReceiveViewModel _fileReceiveViewModel;
        public ClientConnectedViewModel(ClientSession clientSession)
        {
            Session = clientSession;
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
                        if (_fileReceiveViewModel != null)
                        {
                            await Session.SendAsync(new Packet<ApplyFileTransferAck>()
                            {
                                MessageType = MessageType.ApplyTrasnferAck,
                                Body = new ApplyFileTransferAck()
                                {
                                    Result = ApplyFileTransferAckResult.TaskExistAndWorking,
                                    FileSendId = packet.Body.FileSendId,
                                    Message = "任务已存在"
                                }
                            }.Serialize());

                            return;
                        }

                        if (string.IsNullOrEmpty(packet.Body.FileName) || string.IsNullOrEmpty(packet.Body.Code)
                            || string.IsNullOrEmpty(packet.Body.FileSendId))
                        {
                            await Session.SendAsync(new Packet<ApplyFileTransferAck>()
                            {
                                MessageType = MessageType.ApplyTrasnferAck,
                                Body = new ApplyFileTransferAck()
                                {
                                    Result = ApplyFileTransferAckResult.Rejected,
                                    FileSendId = packet.Body.FileSendId,
                                    Message = "传输文件数据异常"
                                }
                            }.Serialize());

                            return;
                        }

                        var fileHelper = App.ServiceProvider.GetRequiredService<FileHelper>();
                        var iniSettings = App.ServiceProvider.GetRequiredService<IniSettings>();
                        var dbHelper = App.ServiceProvider.GetRequiredService<DBHelper>();
                        var canSave = fileHelper.PathCanSave(iniSettings.FileSaveLocation, packet.Body.TotalSize);
                        if (!canSave)
                        {
                            await Session.SendAsync(new Packet<ApplyFileTransferAck>()
                            {
                                MessageType = MessageType.ApplyTrasnferAck,
                                Body = new ApplyFileTransferAck()
                                {
                                    FileSendId = packet.Body.FileSendId,
                                    Result = ApplyFileTransferAckResult.Rejected,
                                    Message = "目标机器存储空间异常"
                                }
                            }.Serialize());

                            return;
                        }

                        var record = await dbHelper
                            .FirstOrDefaultAsync<FileReceiveRecordModel>(x => x.FileSendId == packet.Body.FileSendId);

                        if (record == null) //新发送的文件TODO 
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
                                                Result = ApplyFileTransferAckResult.Rejected,
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
                            _fileReceiveViewModel = new FileReceiveViewModel(task); //通过数据库接收文件模型创建接收文件视图模型
                            WeakReferenceMessenger.Default.Send(_fileReceiveViewModel, "AddReceiveFileRecord");
                            await Session.SendAsync(new Packet<ApplyFileTransferAck>()
                            {
                                MessageType = MessageType.ApplyTrasnferAck,
                                Body = new ApplyFileTransferAck()
                                {
                                    Result = ApplyFileTransferAckResult.Approved,
                                    FileSendId = packet.Body.FileSendId,
                                }
                            }.Serialize());
                        }
                        else //断点续传
                        {
                            if (record.Status == FileReceiveStatus.Completed
                                || record.Status == FileReceiveStatus.Faild)
                            {
                                await Session.SendAsync(new Packet<ApplyFileTransferAck>()
                                {
                                    MessageType = MessageType.ApplyTrasnferAck,
                                    Body = new ApplyFileTransferAck()
                                    {
                                        FileSendId = packet.Body.FileSendId,
                                        Result = ApplyFileTransferAckResult.TaskCompleted,
                                        Message = "任务已完成"
                                    }
                                }.Serialize());

                                return;
                            }

                            if (record.Status == FileReceiveStatus.Transfering)
                            {
                                await Session.SendAsync(new Packet<ApplyFileTransferAck>()
                                {
                                    MessageType = MessageType.ApplyTrasnferAck,
                                    Body = new ApplyFileTransferAck()
                                    {
                                        FileSendId = packet.Body.FileSendId,
                                        Result = ApplyFileTransferAckResult.TaskExistAndWorking,
                                        Message = "任务正在传输中"
                                    }
                                }.Serialize());

                                return;
                            }

                            var vm = App.ServiceProvider.GetRequiredService<ReceiveFilePageViewModel>()
                                .FileReceiveViewModels.FirstOrDefault(x => x.FileSendId == record.FileSendId);
                            _fileReceiveViewModel = vm;
                            if (vm == null)
                            {
                                await Session.SendAsync(new Packet<ApplyFileTransferAck>()
                                {
                                    MessageType = MessageType.ApplyTrasnferAck,
                                    Body = new ApplyFileTransferAck()
                                    {
                                        FileSendId = packet.Body.FileSendId,
                                        Result = ApplyFileTransferAckResult.TaskCompleted,
                                        Message = "任务已完成"
                                    }
                                }.Serialize());
                                await dbHelper.UpdateFileReceiveRecordCompleteAsync(record.Id, null, true);
                                return;
                            }

                            if (!File.Exists(record.TempFileSaveLocation))
                            {
                                await Session.SendAsync(new Packet<ApplyFileTransferAck>()
                                {
                                    MessageType = MessageType.ApplyTrasnferAck,
                                    Body = new ApplyFileTransferAck()
                                    {
                                        FileSendId = packet.Body.FileSendId,
                                        Result = ApplyFileTransferAckResult.DataError,
                                        Message = "数据异常"
                                    }
                                }.Serialize());
                                await vm.ErrorCompletedAsync("临时文件异常");
                                return;
                            }

                            FileInfo fileInfo = new FileInfo(record.TempFileSaveLocation);
                            await Session.SendAsync(new Packet<ApplyFileTransferAck>()
                            {
                                MessageType = MessageType.ApplyTrasnferAck,
                                Body = new ApplyFileTransferAck()
                                {
                                    Result = ApplyFileTransferAckResult.Approved,
                                    FileSendId = packet.Body.FileSendId,
                                    TransferedBytes = fileInfo.Length
                                }
                            }.Serialize());
                        }
                    }
                    break;
                case MessageType.FileSend:
                    {
                        var packet = Packet<FileSegement>.FromBytes(data);
                        var result = await _fileReceiveViewModel?.ReceiveDataAsync(packet.Body);
                        if (result.Item1) 
                        {
                            await Session.DisposeAsync();
                        }
                    }
                    break;
                case MessageType.CancelSend:
                    {
                        var packet = Packet<CancelTransfer>.FromBytes(data);
                        await _fileReceiveViewModel?.PassiveCancelAsync(packet.Body);
                        await Session.DisposeAsync();
                    }
                    break;
            }
        }
    }
}
