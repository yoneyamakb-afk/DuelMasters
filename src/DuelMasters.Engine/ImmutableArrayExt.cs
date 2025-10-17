
using System.Collections.Immutable;

namespace DuelMasters.Engine;

public static class ImmutableArrayExt
{
    public static ImmutableArray<T> RemoveRange<T>(this ImmutableArray<T> arr, int index, int length)
    {
        var builder = arr.ToBuilder();
        builder.RemoveRange(index, length);
        return builder.ToImmutable();
    }
}
