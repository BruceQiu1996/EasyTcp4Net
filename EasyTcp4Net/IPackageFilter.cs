using System.Buffers;

namespace EasyTcp4Net
{
    public interface IPackageFilter
    {
        ReadOnlySequence<byte> ResolvePackage(ref ReadOnlySequence<byte> sequence);
    }
}
