using System;
using System.Collections.Generic;

namespace ProtoFluxContextualActions.Extensions;

public static class DictionaryExtensions
{
  public static V GetOrCreate<K, V>(this IDictionary<K, V> dictionary, K key, Func<V> createValue)
  {
    if (!dictionary.TryGetValue(key, out var value))
    {
      value = createValue();
      dictionary.Add(key, value);
    }
    return value;
  }
}
