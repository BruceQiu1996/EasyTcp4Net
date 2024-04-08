using System.Collections;
using System.Net;
using System.Net.NetworkInformation;
using System.Text.RegularExpressions;
using System.Text;

namespace FileTransfer.Helpers
{
    public class NetHelper
    {
        public int GetFirstAvailablePort()
        {
            int MAX_PORT = 6000; //系统tcp/udp端口数最大是65535           
            int BEGIN_PORT = 5000;//从这个端口开始检测

            for (int i = BEGIN_PORT; i < MAX_PORT; i++)
            {
                if (PortIsAvailable(i)) return i;
            }

            return -1;
        }

        private IList PortIsUsed()
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

        internal bool PortIsAvailable(int port)
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

        public bool MatchIP(string ip)
        {
            bool success = false;
            if (!string.IsNullOrEmpty(ip))
            {
                //判断是否为IP
                success = Regex.IsMatch(ip, @"^((2[0-4]\d|25[0-5]|[01]?\d\d?)\.){3}(2[0-4]\d|25[0-5]|[01]?\d\d?)$");
            }
            return success;
        }

        public bool CheckIPCanPing(string strIP)
        {
            if (!string.IsNullOrEmpty(strIP))
            {
                if (!MatchIP(strIP))
                {
                    return false;
                }

                Ping pingSender = new Ping();
                PingOptions options = new PingOptions();

                // 使用默认的128位值
                options.DontFragment = true;

                //创建一个32字节的缓存数据发送进行ping
                string data = "testtesttesttesttesttesttesttest";
                byte[] buffer = Encoding.ASCII.GetBytes(data);
                int timeout = 120;
                PingReply reply = pingSender.Send(strIP, timeout, buffer, options);

                return (reply.Status == IPStatus.Success);
            }
            else
            {
                return false;
            }
        }

        public bool MutiCheckIPIsPing(string strIP, int waitMilliSecond, int testNumber)
        {
            for (int i = 0; i < testNumber; i++)
            {
                if (CheckIPCanPing(strIP))
                {
                    return true;
                }

                Thread.Sleep(waitMilliSecond);
            }

            return false;
        }
    }
}
