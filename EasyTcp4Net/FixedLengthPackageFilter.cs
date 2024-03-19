using System.Buffers;

namespace EasyTcp4Net
{
    /// <summary>
    /// 固定长度报文解析器
    /// </summary>
    public class FixedLengthPackageFilter : AbstractPackageFilter
    {
        private readonly int _packetSize;
        public FixedLengthPackageFilter(int packetSize)
        {
            _packetSize = packetSize;
        }

        public override ReadOnlySequence<byte> ResolvePackage(ref ReadOnlySequence<byte> sequence)
        {
            if (sequence.Length < _packetSize) return default;
            byte[] bodyLengthBytes = ArrayPool<byte>.Shared.Rent(_packetSize);
            try
            {
                var endPosition = sequence.GetPosition(_packetSize);
                var data = sequence.Slice(0, endPosition);
                sequence = sequence.Slice(endPosition);

                return data;
            }
            finally
            {
                ArrayPool<byte>.Shared.Return(bodyLengthBytes);
            }
        }
    }
}
