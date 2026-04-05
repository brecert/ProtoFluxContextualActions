using System;
using System.Collections.Generic;
using System.Linq;
using Elements.Core;
using ProtoFlux.Core;
using ProtoFlux.Runtimes.Execution.Nodes.FrooxEngine.Time;
using ProtoFlux.Runtimes.Execution.Nodes.Math;
using ProtoFluxContextualActions.Utils;

namespace ProtoFluxContextualActions.Patches;

static partial class ContextualSwapActionsPatch
{
  internal static IEnumerable<MenuItem> ClampGroupItems(ContextualContext context)
  {
    var psuedoGenericTypes = context.World.GetPsuedoGenericTypesForWorld();
    Type? thisType = null;
    UniLog.Warning($"Input. checking clamp types, input is '{context.NodeType.GetNiceFullName()}'");
    if (context.NodeType.IsGenericType && context.NodeType.GetGenericTypeDefinition() == typeof(ValueClamp<>)) thisType = context.NodeType.GenericTypeArguments.First();
    else
    {
      var clampTypes = psuedoGenericTypes.Clamp01.ToDictionary();
      if (clampTypes.TryGetValue(context.NodeType, out var genericTypes))
      {
        thisType = genericTypes.First();
      }
    }
    UniLog.Warning($"Finished checking clamp types, type is '{thisType.GetNiceTypeName()}'");
    if (thisType != null)
    {
      if (psuedoGenericTypes.Clamp01.Any(n => n.Types.First() == thisType))
      {
        yield return new(psuedoGenericTypes.Clamp01.First(n => n.Types.First() == thisType).Node);
      }
      if (thisType.IsValueType)
      {
        yield return new(typeof(ValueClamp<>).MakeGenericType(thisType));
      }
    }
  }
}