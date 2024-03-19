using System.Buffers;

namespace EasyTcp4Net
{
    public abstract class AbstractPackageFilter : IPackageFilter
    {
        public AbstractPackageFilter() { }

        public abstract ReadOnlySequence<byte> ResolvePackage(ref ReadOnlySequence<byte> sequence);
    }
}
