using FrooxEngine;

namespace ProtoFluxContextualActions.Extensions;

internal static class ISyncListExtensions
{
  public static void EnsureElementCount(this ISyncList syncList, int size)
  {
    if (syncList.Count < size) syncList.EnsureExactElementCount(size);
  }
}
