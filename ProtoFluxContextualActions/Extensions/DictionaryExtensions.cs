using System;
using System.Collections.Generic;

namespace ProtoFluxContextualActions.Extensions;

public static class DictionaryExtensions
{
  public static void Add<K, V>(this Dictionary<K, List<V>> dictionary, K key, V value)
  {
    if (dictionary.TryGetValue(key, out var collection))
    {
      collection.Add(value);
    }
    else
    {
      dictionary[key] = [value];
    }
  }

  public static V GetOrCreate<K, V>(this IDictionary<K, V> dictionary, K key, Func<V> createValue)
  {
    if (!dictionary.TryGetValue(key, out var value))
    {
      value = createValue();
      dictionary.Add(key, value);
    }
    return value;
  }

  public static V GetValueOrDefault<K, V>(this IDictionary<K, V> dictionary, K key, V defaultValue) =>
    dictionary.TryGetValue(key, out var value) ? value : defaultValue;

  public static V GetValueOrDefault<K, V>(this IDictionary<K, V> dictionary, K key, Func<V> defaultValue) =>
    dictionary.TryGetValue(key, out var value) ? value : defaultValue();

}
