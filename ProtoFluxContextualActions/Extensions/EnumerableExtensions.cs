using System;
using System.Collections.Generic;
using Elements.Core;

namespace ProtoFluxContextualActions.Extensions;

public static class EnumerableExtensions
{
  public static BiDictionary<TKey, TElement> ToBiDictionary<TKey, TElement>(this IEnumerable<(TKey, TElement)> source) where TKey : notnull
  {
    var dictionary = new BiDictionary<TKey, TElement>();
    foreach (var (k, v) in source)
    {
      dictionary.Add(k, v);
    }
    return dictionary;
  }


  public static BiDictionary<TKey, TElement> ToBiDictionary<TSource, TKey, TElement>(this IEnumerable<TSource> source, Func<TSource, TKey> keySelector, Func<TSource, TElement> elementSelector) where TKey : notnull
  {
    var dictionary = new BiDictionary<TKey, TElement>();
    foreach (var item in source)
    {
      var key = keySelector(item);
      var element = elementSelector(item);
      dictionary.Add(key, element);
    }
    return dictionary;
  }
}
