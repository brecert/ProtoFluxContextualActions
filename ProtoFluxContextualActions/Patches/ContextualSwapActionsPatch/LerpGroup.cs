using System;
using System.Collections.Generic;
using System.Linq;
using Elements.Core;
using ProtoFlux.Core;
using ProtoFlux.Runtimes.Execution.Nodes.FrooxEngine.Time;
using ProtoFlux.Runtimes.Execution.Nodes.Math;
using ProtoFlux.Runtimes.Execution.Nodes.Math.Quaternions;
using ProtoFluxContextualActions.Utils;
using static ProtoFluxContextualActions.Utils.PsuedoGenericUtils;

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

  static readonly HashSet<Type> SmoothSlerpGroup = [
    typeof(SmoothSlerp_floatQ),
    typeof(SmoothSlerp_doubleQ),
    typeof(ConstantSlerp_floatQ),
    typeof(ConstantSlerp_doubleQ),
  ];

  internal static IEnumerable<MenuItem> LerpGroupItems(ContextualContext context)
  {
    var world = context.hitNode.World;
    var psuedoGenericTypes = world.GetPsuedoGenericTypesForWorld();

    Type nodeValueType = GetTypesFromNode(world, context.NodeType).First();

    var CosineLerpNodes = psuedoGenericTypes.CosineLerp;
    var MultiCosineLerpNodes = psuedoGenericTypes.MultiCosineLerp;

    var BezierCurveNodes = psuedoGenericTypes.BezierCurve;
    var MultiBezierCurveNodes = psuedoGenericTypes.MultiBezierCurve;

    var SlerpNodes = psuedoGenericTypes.Slerp;
    var SlerpUnclampedNodes = psuedoGenericTypes.SlerpUnclamped;

    var SlerpWithMagnitudeNodes = psuedoGenericTypes.SlerpWithMagnitude;

    if (
        LerpGroup.Any(t => context.NodeType.IsGenericType ? t == context.NodeType.GetGenericTypeDefinition() : t == context.NodeType)
        || SlerpNodes.Any(t => t.Node == context.NodeType) || SlerpUnclampedNodes.Any(t => t.Node == context.NodeType) || SlerpWithMagnitudeNodes.Any(t => t.Node == context.NodeType)
        || CosineLerpNodes.Any(t => t.Node == context.NodeType) || MultiCosineLerpNodes.Any(t => t.Node == context.NodeType)
      )
    {
      foreach (var match in LerpGroup)
      {
        yield return new MenuItem(
          match.MakeGenericType(nodeValueType),
          name: match == typeof(ValueMultiLerp<>) ? "Multi Lerp" : null,
          connectionTransferType: match == typeof(ValueMultiLerp<>) ? ConnectionTransferType.ByMappingsLossy : ConnectionTransferType.ByNameLossy);
      }
      if (SlerpNodes.Any(t => t.Types.SequenceEqual([nodeValueType])))
      {
        yield return new MenuItem(SlerpNodes.First(t => t.Types.SequenceEqual([nodeValueType])).Node);
      }
      if (SlerpUnclampedNodes.Any(t => t.Types.SequenceEqual([nodeValueType])))
      {
        yield return new MenuItem(SlerpUnclampedNodes.First(t => t.Types.SequenceEqual([nodeValueType])).Node);
      }
      if (SlerpWithMagnitudeNodes.Any(t => t.Types.SequenceEqual([nodeValueType])))
      {
        yield return new MenuItem(SlerpWithMagnitudeNodes.First(t => t.Types.SequenceEqual([nodeValueType])).Node);
      }
      if (CosineLerpNodes.Any(t => t.Types.SequenceEqual([nodeValueType])))
      {
        yield return new MenuItem(CosineLerpNodes.First(t => t.Types.SequenceEqual([nodeValueType])).Node);
      }
      if (MultiCosineLerpNodes.Any(t => t.Types.SequenceEqual([nodeValueType])))
      {
        yield return new MenuItem(MultiCosineLerpNodes.First(t => t.Types.SequenceEqual([nodeValueType])).Node, name: "Multi Cosine Lerp");
      }
    }
    if (SmoothLerpGroup.Concat(SmoothSlerpGroup).Any(t => context.NodeType.IsGenericType ? t == context.NodeType.GetGenericTypeDefinition() : t == context.NodeType))
    {
      foreach (var match in SmoothLerpGroup)
      {
        yield return new MenuItem(match.MakeGenericType(nodeValueType));
      }
      foreach (var match2 in SmoothSlerpGroup.Where(t => GetTypesFromNode(world, t).First() == nodeValueType))
      {
        yield return new MenuItem(match2);
      }
    }
    if (BezierCurveNodes.Any(t => t.Node == context.NodeType) || MultiBezierCurveNodes.Any(t => t.Node == context.NodeType))
    {
      if (BezierCurveNodes.Any(t => t.Types.SequenceEqual([nodeValueType])))
      {
        yield return new MenuItem(BezierCurveNodes.First(t => t.Types.SequenceEqual([nodeValueType])).Node);
      }
      if (MultiBezierCurveNodes.Any(t => t.Types.SequenceEqual([nodeValueType])))
      {
        yield return new MenuItem(MultiBezierCurveNodes.First(t => t.Types.SequenceEqual([nodeValueType])).Node);
      }
    }
  }
}