using System.Collections.ObjectModel;

namespace EAP.Gateway.Infrastructure.Extensions;

public static class DictionaryExtensions
{
    public static IReadOnlyDictionary<TKey, TValue> ToReadOnlyDictionary<TKey, TValue>(
        this IDictionary<TKey, TValue> dictionary) where TKey : notnull
    {
        return new ReadOnlyDictionary<TKey, TValue>(dictionary);
    }
}
