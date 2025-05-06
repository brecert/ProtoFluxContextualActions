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
}
