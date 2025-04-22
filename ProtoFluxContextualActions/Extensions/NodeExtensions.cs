using System.Collections.Generic;
using ProtoFlux.Core;

namespace ProtoFluxContextualActions.Extensions;

internal static class NodeExtensions
{
  public static IEnumerable<IOutput> AllInputsSources(this INode node)
  {
    for (int i = 0; i < node.InputCount; i++)
    {
      yield return node.GetInputSource(i);
    }
  }
}
