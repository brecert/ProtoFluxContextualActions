using Elements.Core;
using FrooxEngine;
using FrooxEngine.ProtoFlux;
using ProtoFlux.Core;
using ProtoFlux.Runtimes.Execution.Nodes.Actions;
using ProtoFlux.Runtimes.Execution.Nodes.FrooxEngine.Animation;
using System;
using System.Collections.Generic;
using System.Linq;

namespace ProtoFluxContextualActions.Patches;

static partial class ContextualSwapActionsPatch
{
  internal static IEnumerable<MenuItem> TweenGroupItems(ContextualContext context)
  {
    if (context.NodeType.IsGenericType ? typeof(TweenValue<>) == context.NodeType.GetGenericTypeDefinition() : typeof(TweenValue<>) == context.NodeType)
    {
      Type? target = null;
      bool hasProxyHeld = false;

      if (context.proxy is ProtoFluxInputProxy)
      {
        ProtoFluxInputProxy inputType = (ProtoFluxInputProxy)context.proxy;
        Type targetType = inputType.InputType;
        target = targetType;
        hasProxyHeld = true;
      }
      if (context.proxy is ProtoFluxOutputProxy)
      {
        ProtoFluxOutputProxy outputType = (ProtoFluxOutputProxy)context.proxy;
        Type targetType = outputType.OutputType;
        target = targetType;
        hasProxyHeld = true;
      }
      if (context.NodeType.IsGenericType && target == null)
      {
        var opCount = context.NodeType.GenericTypeArguments.Length;
        var opType = context.NodeType.GenericTypeArguments[opCount - 1];
        target = opType;
      }

      if (hasProxyHeld && target != null)
      {
        yield return new(typeof(TweenValue<>).MakeGenericType(target));
      }
    }
  }
}