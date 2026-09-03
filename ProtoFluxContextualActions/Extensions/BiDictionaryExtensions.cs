using Elements.Core;

namespace ProtoFluxContextualActions.Extensions;

public static class BiDictionaryExtensions
{
  public static bool TryGetEither<T>(this BiDictionary<T, T> dict, T key, out T value) =>
    dict.TryGetSecond(key, out value) || dict.TryGetFirst(key, out value);

  public static bool ContainsEither<T>(this BiDictionary<T, T> dict, T key) =>
    dict.ContainsSecond(key) || dict.ContainsFirst(key);
}
