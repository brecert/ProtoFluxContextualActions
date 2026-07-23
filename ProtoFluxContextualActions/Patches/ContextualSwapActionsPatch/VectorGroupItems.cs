using System;
using System.Collections.Generic;
using System.Linq;
using ProtoFluxContextualActions.Utils;

namespace ProtoFluxContextualActions.Patches;

static partial class ContextualSwapActionsPatch
{
  internal static IEnumerable<MenuItem> VectorGroupItems(ContextualContext context)
  {
    var psuedoGenericTypes = context.World.GetPsuedoGenericTypesForWorld();
    var magnitude = psuedoGenericTypes.Magnitude;
    var normalized = psuedoGenericTypes.Normalized;
    IEnumerable<(Type Node, IEnumerable<Type> Types)> allTypes = [.. magnitude, .. normalized];
    if (allTypes.Any(n => n.Node == context.NodeType))
    {
      var type = allTypes.First(n => n.Node == context.NodeType).Types.First();
      if (magnitude.Any(n => n.Types.First() == type)) yield return new(magnitude.First(n => n.Types.First() == type).Node);
      if (normalized.Any(n => n.Types.First() == type)) yield return new(normalized.First(n => n.Types.First() == type).Node);
    }
  }
}