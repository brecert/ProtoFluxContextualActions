using System.Collections.Generic;

namespace ProtoFluxContextualActions.Extensions;

internal static class GenericCollectionExtensions
{
  public static void Deconstruct<T1, T2>(this KeyValuePair<T1, T2> pair, out T1 key, out T2 value)
  {
    key = pair.Key;
    value = pair.Value;
  }
}
