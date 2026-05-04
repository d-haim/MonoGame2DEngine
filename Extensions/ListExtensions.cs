using System.Collections.Generic;

namespace MonoGameEngine.Extensions;

public static class ListExtensions
{
    /// <summary>
    /// Shallow copy a list to avoid GC
    /// </summary>
    /// <typeparam name="T"></typeparam>
    /// <param name="source"></param>
    /// <returns></returns>
    public static ICollection<T> ShallowCopy<T>(this ICollection<T> source)
    {
        var destination = new List<T>();
        destination.EnsureCapacity(source.Count);
        destination.AddRange(source);
        return destination;
    }
}