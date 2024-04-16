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
using Microsoft.Extensions.DependencyInjection;
using System.Buffers;
using System.Buffers.Binary;
using System.IO;
using System.Windows.Media.Imaging;

namespace FileTransfer.ViewModels
{
    public class FileSendViewModel : ObservableObject, IAsyncDisposable
    {
        public string Id { get; set; }
        public string RemoteId { get; set; }
        public string FileName { get; set; }
        public string FileLocation { get; set; }
        public string Code { get; set; }
        private FileSendStatus _status;
        public FileSendStatus Status
        {
            get => _status;
            set
            {
                SetProperty(ref _status, value);
                Pausing = Status == FileSendStatus.Pausing;
            }
        }
        public BitmapImage Icon => App.ServiceProvider!.GetRequiredService<FileHelper>().GetIconByFileExtension(FileName).Item2;
        public long Size { get; set; } //发送文件的大小
        private long _transferBytes;
        //已经传输的字节
        public long TransferBytes
        {
            get => _transferBytes;
            set
            {
                _transferBytes = value;
                Progress = TransferBytes / (double)Size * 100;
            }
        }
        public string SizeText => App.ServiceProvider!.GetRequiredService<FileHelper>().ToSizeText(Size);
        private double _progress;
        //传输的进度
        public double Progress
        {
            get => _progress;
            set
            {
                SetProperty(ref _progress, value);
            }
        }

        public string Remote { get; set; } //远端服务器描述
        private bool Connected { get; set; } //是否连接上远端
        private readonly EasyTcpClient _easyTcpClient;
        private readonly CancellationTokenSource _cancellationTokenSource = new CancellationTokenSource(); //取消
        private readonly ManualResetEvent _resetEvent = new ManualResetEvent(true); //暂停

        public RelayCommand PauseCommand { get; set; }
        public RelayCommand ContinueCommand { get; set; }
        public AsyncRelayCommand CancelCommandAsync { get; set; }
        public FileSendViewModel(RemoteChannelModel remoteChannelModel)
        {
            _easyTcpClient = new EasyTcpClient(remoteChannelModel.IPAddress, remoteChannelModel.Port, new EasyTcpClientOptions()
            {
                ConnectRetryTimes = 3,
                BufferSize = 4 * 1024 * 100
            });
            _easyTcpClient.SetReceiveFilter(new FixedHeaderPackageFilter(16, 8, 4, false));
            _easyTcpClient.OnDisConnected += (obj, e) =>
            {
                Connected = false;
            };
            _easyTcpClient.OnReceivedData += async (obj, e) =>
            {
                await HandleMessageAsync(e.Data);
            };
            PauseCommand = new RelayCommand(Pause);
            ContinueCommand = new RelayCommand(Continue);
            CancelCommandAsync = new AsyncRelayCommand(CancelAsync);
        }

        /// <summary>
        /// 从数据库或者已有记录还原视图模型
        /// </summary>
        /// <param name="model">持久化发送记录模型</param>
        /// <param name="remoteChannelModel">远端数据模型</param>
        public FileSendViewModel(FileSendRecordModel model, RemoteChannelModel remoteChannelModel) : this(remoteChannelModel)
        {
            Id = model.Id;
            RemoteId = remoteChannelModel.Id;
            FileName = model.FileName;
            FileLocation = model.FileLocation;
            Status = model.Status;
            Size = model.TotalSize;
            TransferBytes = model.TransferedSize;
            Code = model.Code;
            Progress = Size == 0 ? 100 : TransferBytes * 100 / Size;
            Remote = $"目标主机：{remoteChannelModel.IPAddress}" + (string.IsNullOrEmpty(remoteChannelModel.Remark) ? "" : $"（{remoteChannelModel.Remark}）");
        }

        private bool pausing;
        public bool Pausing
        {
            get => pausing;
            set
            {
                SetProperty(ref pausing, value);
            }
        }

        private void Pause()
        {
            if (Pausing)
                return;
            _resetEvent.Reset();
            Status = FileSendStatus.Pausing;
        }

        private void Continue()
        {
            if (!Pausing)
                return;
            _resetEvent.Set();
            Status = FileSendStatus.Transfering;
        }

        private async Task CancelAsync()
        {
            _resetEvent.Set();
            _cancellationTokenSource?.Cancel();
            await CancelCompletedAsync();
        }

        /// <summary>
        /// 发送文件数据
        /// </summary>
        /// <param name="startIndex"></param>
        /// <param name="easyTcpClient"></param>
        /// <returns></returns>
        public async Task SendAsync()
        {
            FileInfo fileInfo = new FileInfo(FileLocation);
            if (TransferBytes >= fileInfo.Length)
            {
                //文件传输完成
                await NormolCompletedAsync();
            }

            int bufferSize = 1024 * 400;
            int segement = 1; //总共的段数
            int segementIndex = 1;
            var fileHelper = App.ServiceProvider.GetRequiredService<FileHelper>();
            long needTransfer = fileInfo.Length - TransferBytes;
            await Task.Run(async () =>
            {
                using (var fileStream = File.OpenRead(FileLocation))
                {
                    byte[] buffer = new byte[bufferSize];
                    //分段
                    if (needTransfer > buffer.Length)
                    {
                        segement = (int)(needTransfer / buffer.Length);
                        if (needTransfer % buffer.Length != 0)
                        {
                            segement++;
                        }
                    }
                    try
                    {
                        var totalLength = fileInfo.Length;
                        fileStream.Seek(TransferBytes, SeekOrigin.Begin);
                        while (TransferBytes < totalLength)
                        {
                            _resetEvent.WaitOne();
                            if (_cancellationTokenSource.IsCancellationRequested)
                            {
                                return;
                            }

                            var length = await fileStream.ReadAsync(buffer);
                            await _easyTcpClient.SendAsync(new Packet<FileSegement>()
                            {
                                MessageType = MessageType.FileSend,
                                Body = new FileSegement()
                                {
                                    Data = buffer[..length],
                                    SegementIndex = segementIndex,
                                    TotalSegement = segement,
                                    FileSendId = Id
                                }
                            }.Serialize());
                            Console.WriteLine($"发送：{Id}---{segementIndex}");
                            segementIndex++;
                            TransferBytes += length;
                        }

                        await NormolCompletedAsync();
                    }
                    catch (Exception ex)
                    {
                        //TODO异常结束发送
                        //结束发送并且暂停发送

                    }
                }
            });
        }

        /// <summary>
        /// 正常任务结束
        /// </summary>
        public async Task NormolCompletedAsync()
        {
            var dbHelper = App.ServiceProvider!.GetRequiredService<DBHelper>();
            //更新数据库
            await dbHelper.UpdateFileSendRecordCompleteAsync(Id, RemoteId, true);
            WeakReferenceMessenger.Default.Send(Id, "SendFinish");
        }

        /// <summary>
        /// 取消导致任务结束
        /// </summary>
        /// <returns></returns>
        public async Task CancelCompletedAsync()
        {
            var dbHelper = App.ServiceProvider!.GetRequiredService<DBHelper>();
            //更新数据库
            await dbHelper.UpdateFileSendRecordCompleteAsync(Id, RemoteId, false, "取消发送");
            //通知接收端
            try
            {
                var _ = Task.Run(async () =>
                {
                    await _easyTcpClient.SendAsync(new Packet<CancelTransfer>()
                    {
                        MessageType = MessageType.CancelSend,
                        Body = new CancelTransfer()
                        {
                            FileSendId = Id
                        }
                    }.Serialize());
                });
            }
            catch { }
            //dispose该任务
            await DisposeAsync();
            WeakReferenceMessenger.Default.Send(Id, "SendFinish");
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
                        Status = FileSendStatus.Pending;

                        await _easyTcpClient.SendAsync(new Packet<ApplyFileTransfer>()
                        {
                            MessageType = MessageType.ApplyTrasnfer,
                            Body = new ApplyFileTransfer(FileName, Id, Size, 0, Code)
                        }.Serialize());
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
                            if (packet.Body.FileSendId == Id)
                            {
                                await SendAsync();
                                Status = FileSendStatus.Transfering;
                            }
                        }
                    }
                    break;
            }
        }

        /// <summary>
        /// 发送新文件
        /// </summary>
        /// <returns></returns>
        public async Task StartSendFileAsync()
        {
            if (!Connected)
            {
                await _easyTcpClient.ConnectAsync();
                Connected = true;
            }
        }

        private bool _disposed;
        public async ValueTask DisposeAsync()
        {
            if (!_disposed)
            {
                await _easyTcpClient?.DisConnectAsync();
                _disposed = true;
            }
        }
    }
}
