using System;
using System.Collections.Generic;
using System.Linq;
using Elements.Core;
using ProtoFluxContextualActions.Utils;

namespace ProtoFluxContextualActions.Patches;

static partial class ContextualSwapActionsPatch
{
  internal static IEnumerable<MenuItem> ZeroOneGroupItems(ContextualContext context)
  {
    var psuedoGenericTypes = context.World.GetPsuedoGenericTypesForWorld();
    var repeat01 = psuedoGenericTypes.Repeat01;
    var remap11_01 = psuedoGenericTypes.Remap11_01;
    var clamp01 = psuedoGenericTypes.Clamp01;
    IEnumerable<(Type Node, IEnumerable<Type> Types)> allTypes = [.. repeat01, .. remap11_01, .. clamp01];
    if (allTypes.Any(n => n.Node == context.NodeType))
    {
      var type = allTypes.First(n => n.Node == context.NodeType).Types.First();
      if (repeat01.Any(n => n.Types.First() == type)) yield return new(repeat01.First(n => n.Types.First() == type).Node);
      if (remap11_01.Any(n => n.Types.First() == type)) yield return new(remap11_01.First(n => n.Types.First() == type).Node);
      if (clamp01.Any(n => n.Types.First() == type)) yield return new(clamp01.First(n => n.Types.First() == type).Node);
    }
  }
}