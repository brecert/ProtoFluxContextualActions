using System;
using System.Collections.Generic;
using System.Linq;
using ProtoFlux.Runtimes.Execution.Nodes.FrooxEngine.Time;
using ProtoFlux.Runtimes.Execution.Nodes.Math;
using ProtoFluxContextualActions.Utils;

namespace ProtoFluxContextualActions.Patches;

static partial class ContextualSwapActionsPatch
{
  static readonly HashSet<Type> LerpGroup = [
    typeof(ValueLerp<>),
    typeof(ValueMultiLerp<>),
    typeof(ValueLerpUnclamped<>),
    typeof(ValueInverseLerp<>),
  ];

  static readonly HashSet<Type> SmoothLerpGroup = [
    typeof(ValueSmoothLerp<>),
    typeof(ValueConstantLerp<>),
  ];

  internal static IEnumerable<MenuItem> LerpGroupItems(ContextualContext context)
  {
    if (LerpGroup.Any(t => context.NodeType.IsGenericType ? t == context.NodeType.GetGenericTypeDefinition() : t == context.NodeType))
    {
      foreach (var match in LerpGroup)
      {
        yield return new MenuItem(
          match.MakeGenericType(context.NodeType.GenericTypeArguments),
          name: match == typeof(ValueMultiLerp<>) ? "Multi Lerp" : null,
          connectionTransferType: match == typeof(ValueMultiLerp<>) ? ConnectionTransferType.ByMappingsLossy : ConnectionTransferType.ByNameLossy);
      }
    }
    if (SmoothLerpGroup.Any(t => context.NodeType.IsGenericType ? t == context.NodeType.GetGenericTypeDefinition() : t == context.NodeType))
    {
      foreach (var match in SmoothLerpGroup)
      {
        yield return new MenuItem(match.MakeGenericType(context.NodeType.GenericTypeArguments));
      }
    }
  }
}