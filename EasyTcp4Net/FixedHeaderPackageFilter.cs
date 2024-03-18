using System.Buffers;

namespace EasyTcp4Net
{
    /// <summary>
    /// 固定数据包头解析器
    /// </summary>
    public class FixedHeaderPackageFilter : AbstractPackageFilter
    {
        private readonly int _headerSize;
        private readonly int _bodyLengthIndex;
        private readonly int _bodyLengthBytes;

        private ReadOnlySequence<byte> _buffer = ReadOnlySequence<byte>.Empty;
        /// <summary>
        /// 固定报文头解析协议
        /// </summary>
        /// <param name="headerSize">数据报文头的大小</param>
        /// <param name="bodyLengthIndex">数据包大小在报文头中的位置</param>
        /// <param name="bodyLengthBytes">数据包大小在报文头中的长度</param>
        /// <param name="IsLittleEndian">数据报文大小端。windows中通常是小端，unix通常是大端模式</param>
        public FixedHeaderPackageFilter(int headerSize, int bodyLengthIndex, int bodyLengthBytes, bool IsLittleEndian = true) : base(IsLittleEndian)
        {
            _headerSize = headerSize;
            _bodyLengthIndex = bodyLengthIndex;
            _bodyLengthBytes = bodyLengthBytes;
        }

        /// <summary>
        /// 解析数据包
        /// </summary>
        /// <param name="sequence"></param>
        public override ReadOnlySequence<byte> ResolvePackage(ref ReadOnlySequence<byte> sequence)
        {
            var len = sequence.Length;
            if (len < _bodyLengthIndex)
            {
                return default;
            }

            var bodyLengthSequence = sequence.Slice(_bodyLengthIndex, _bodyLengthBytes);
            byte[] bodyLengthBytes = ArrayPool<byte>.Shared.Rent(_bodyLengthBytes);
            try
            {
                int index = 0;
                foreach (var item in bodyLengthSequence)
                {
                    Array.Copy(item.ToArray(), 0, bodyLengthBytes, index, item.Length);
                    index += item.Length;
                }

                var bodyLength = BitConverter.ToInt32(bodyLengthBytes);
                if (sequence.Length < _headerSize + bodyLength)
                    return default;

                var endPosition = sequence.GetPosition(_headerSize + bodyLength);
                var data = sequence.Slice(0, _headerSize + bodyLength);
                sequence = sequence.Slice(endPosition);

                return data.Slice(0, data.Length);
            }
            finally
            {
                ArrayPool<byte>.Shared.Return(bodyLengthBytes);
            }
        }
    }
}
