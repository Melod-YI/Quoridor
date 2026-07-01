using System.Collections.Immutable;

namespace Quoridor.Domain.Core;

public static class ImmutableArrayExtensions
{
    public static ImmutableArray<T> Replace<T>(this ImmutableArray<T> arr, T old, T @new)
        where T : class
    {
        var b = ImmutableArray.CreateBuilder<T>(arr.Length);
        bool replaced = false;
        foreach (var x in arr)
        {
            if (!replaced && x.Equals(old)) { b.Add(@new); replaced = true; }
            else b.Add(x);
        }
        return b.MoveToImmutable();
    }
}
