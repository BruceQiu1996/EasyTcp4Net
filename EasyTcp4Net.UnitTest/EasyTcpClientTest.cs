using Moq;
using System.Net;
using System.Net.Sockets;

namespace EasyTcp4Net.UnitTest
{
    [TestFixture]
    public class EasyTcpClientTest
    {
        [SetUp]
        public void Setup() { }

        [Test]
        public void Ctor_WhenServerHostIsNull_ArgumentNullException()
        {
            string serverHost = null;
            Assert.Throws<ArgumentNullException>(() => { EasyTcpClient easyTcpClientTest = new EasyTcpClient(serverHost, 1000); });
        }

        [Test]
        public void Ctor_WhenPortEqualZero_InvalidDataException()
        {
            string serverHost = "test";
            ushort port = 0;
            Assert.Throws<InvalidDataException>(() => { EasyTcpClient easyTcpClientTest = new EasyTcpClient(serverHost, port); });
        }

        [Test]
        public void Ctor_SslTrueButFileIsEmpty_SslObjectNull()
        {
            string serverHost = "test";
            ushort port = 1000;
            EasyTcpClientOptions options = new EasyTcpClientOptions()
            {
                IsSsl = true,
                PfxCertFilename = null
            };

            EasyTcpClient easyTcpClient = new EasyTcpClient(serverHost, port, options);
            Assert.IsNull(easyTcpClient.Certificate);
        }

        [Test]
        public void Connect_Outof3Seconds_Exception()
        {
            string serverHost = "test";
            ushort port = 1000;
            EasyTcpClient easyTcpClient = new EasyTcpClient(serverHost, port);
            Assert.ThrowsAsync<SocketException>(easyTcpClient.ConnectAsync);
        }
    }
}