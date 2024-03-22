namespace EasyTcp4Net.UnitTest
{
    [TestFixture]
    public class EasyTcpClientTest
    {
        [SetUp]
        public void Setup()
        {

        }

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
    }
}