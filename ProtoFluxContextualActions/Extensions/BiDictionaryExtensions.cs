using System;
using System.Collections.Generic;
using Elements.Core;

namespace ProtoFluxContextualActions.Extensions;

public static class BiDictionaryExtensions
{
  public static bool TryGetEither<T>(BiDictionary<T, T> swaps, T key, out T value) =>
    swaps.TryGetSecond(key, out value) || swaps.TryGetFirst(key, out value);
}
