using System.Buffers;

namespace EasyTcp4Net
{
    public interface IPackageFilter
    {
        ReadOnlySequence<byte> ResolvePackage(ReadOnlySequence<byte> package);
    }
}
