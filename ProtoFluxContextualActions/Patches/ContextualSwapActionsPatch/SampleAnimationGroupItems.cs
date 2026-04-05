using Elements.Core;
using FrooxEngine.ProtoFlux;
using ProtoFlux.Core;
using ProtoFlux.Runtimes.Execution.Nodes.Actions;
using ProtoFlux.Runtimes.Execution.Nodes.FrooxEngine.Assets;
using ProtoFlux.Runtimes.Execution.Nodes.FrooxEngine.Variables;
using System;
using System.Collections.Generic;
using System.Linq;

namespace ProtoFluxContextualActions.Patches;

static partial class ContextualSwapActionsPatch
{
  static readonly HashSet<Type> SampleAnimationGroup = [
    typeof(SampleValueAnimationTrack<>),
    typeof(SampleObjectAnimationTrack<>),
  ];


  internal static IEnumerable<MenuItem> SampleAnimationGroupItems(ContextualContext context)
  {
    if (SampleAnimationGroup.Any(t => context.NodeType.IsGenericType ? t == context.NodeType.GetGenericTypeDefinition() : t == context.NodeType))
    {
      Type? target = null;

      if (context.proxy is ProtoFluxInputProxy)
      {
        ProtoFluxInputProxy inputType = (ProtoFluxInputProxy)context.proxy;
        Type targetType = inputType.InputType;
        target = targetType;
      }
      if (context.proxy is ProtoFluxOutputProxy)
      {
        ProtoFluxOutputProxy outputType = (ProtoFluxOutputProxy)context.proxy;
        Type targetType = outputType.OutputType;
        target = targetType;
      }

      if (target != null)
      {
        var AnimType = GetNodeForType(target, [
          new NodeTypeRecord(typeof(SampleValueAnimationTrack<>), null, null),
          new NodeTypeRecord(typeof(SampleObjectAnimationTrack<>), null, null),
        ]);
        if (AnimType.IsValidGenericType(true))
        {
          yield return new(AnimType, name: $"Sample {(AnimType.GetGenericTypeDefinition() == typeof(SampleValueAnimationTrack<>) ? "Value" : "Object")} Animation Track <{target.GetNiceTypeName()}>");
        }
      }
    }
  }
}