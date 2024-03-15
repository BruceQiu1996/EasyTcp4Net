using CommunityToolkit.HighPerformance.Buffers;
using System;
using System.Buffers;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EasyTcp4Net
{
    public class FixedHeaderPackageFilter : IPackageFilter
    { 
        private readonly int _headerSize;
        private readonly int _bodyLengthIndex;
        private readonly int _bodyLengthBytes;
        private readonly bool _isLittleEndian;

        private ReadOnlySequence<byte> _buffer = ReadOnlySequence<byte>.Empty;
        /// <summary>
        /// 固定报文头解析协议
        /// </summary>
        /// <param name="headerSize">数据报文头的大小</param>
        /// <param name="bodyLengthIndex">数据包大小在报文头中的位置</param>
        /// <param name="bodyLengthBytes">数据包大小在报文头中的长度</param>
        /// <param name="IsLittleEndian">数据报文大小端。windows中通常是小端，unix通常是大端模式</param>
        public FixedHeaderPackageFilter(int headerSize, int bodyLengthIndex, int bodyLengthBytes, bool IsLittleEndian = true)
        {
            _headerSize = headerSize;
            _bodyLengthIndex = bodyLengthIndex;
            _bodyLengthBytes = bodyLengthBytes;
            _isLittleEndian = IsLittleEndian;
        }

        public Packet ResolvePackage(Memory<byte> package)
        {
            _buffer = new ReadOnlySequence<byte>(package);
            _buffer.
        }
    }
}
