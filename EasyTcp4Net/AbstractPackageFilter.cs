using System.Buffers;

namespace EasyTcp4Net
{
    public abstract class AbstractPackageFilter : IPackageFilter
    {
        protected bool IsLittleEndian { get; set; }
        public AbstractPackageFilter(bool isLittleEndian)
        {
            IsLittleEndian = isLittleEndian;
        }

        public abstract ReadOnlySequence<byte> ResolvePackage(ref ReadOnlySequence<byte> sequence);
    }
}
