using EasyTcp4Net;
using FileSenderCommon.Dtos;
using System.Collections.Concurrent;

namespace FileSenderServer
{
    /// <summary>
    /// 局域网文件发送服务端
    /// </summary>
    internal class Program
    {
        private static ConcurrentDictionary<string, ClientSession> clientSessions = new ConcurrentDictionary<string, ClientSession>();
        static void Main(string[] args)
        {
            EasyTcpServer easyTcpServer = new EasyTcpServer(7007);
            easyTcpServer.SetReceiveFilter(new FixedHeaderPackageFilter(16, 8, 4, false));
            easyTcpServer.StartListen();

            easyTcpServer.OnClientConnectionChanged += async (obj, e) =>
            {
                if (e.Status == ConnectsionStatus.Connected)
                {
                    clientSessions.TryAdd(e.ClientSession.RemoteEndPoint.ToString(), e.ClientSession);
                }
                else
                {
                    clientSessions.TryRemove(e.ClientSession.RemoteEndPoint.ToString(), out var session);
                }

                var members = clientSessions.Select(x => new Member()
                {
                    IPAddress = x.Value.RemoteEndPoint.Address.ToString(),
                    Port = (ushort)x.Value.RemoteEndPoint.Port,
                    SessionId = x.Value.SessionId
                }).ToList();

                foreach (var session in clientSessions) 
                {
                    await session.Value.SendAsync(new BasePacket<MembersPushMessage>() 
                    {
                        MessageType = MessageType.MembersPush,
                        Body = new MembersPushMessage() 
                        {
                            Members = members
                        }
                    }.Serialize());
                }
            };

            easyTcpServer.OnReceivedData += (obj, e) =>
            {

            };

            Console.Read();
        }
    }
}
