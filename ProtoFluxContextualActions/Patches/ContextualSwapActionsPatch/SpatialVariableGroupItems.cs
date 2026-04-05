using Elements.Core;
using FrooxEngine.ProtoFlux;
using ProtoFlux.Core;
using ProtoFlux.Runtimes.Execution.Nodes.Actions;
using ProtoFlux.Runtimes.Execution.Nodes.FrooxEngine.Variables;
using System;
using System.Collections.Generic;
using System.Linq;

namespace ProtoFluxContextualActions.Patches;

static partial class ContextualSwapActionsPatch
{
  static readonly HashSet<Type> SpatialVariableGroup = [
    typeof(SampleBooleanSpatialVariable),
    typeof(SampleValueSpatialVariable<>),
    typeof(SampleObjectSpatialVariable<>),
    typeof(SampleNumericSpatialVariable<>),
    typeof(SampleMinMaxSpatialVariable<>),
    typeof(SampleSpatialVariablePartialDerivative<>),
  ];


  internal static IEnumerable<MenuItem> SpatialVariableGroupItems(ContextualContext context)
  {
    if (SpatialVariableGroup.Any(t => context.NodeType.IsGenericType ? t == context.NodeType.GetGenericTypeDefinition() : t == context.NodeType))
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

      if (target != null)
      {
        if (target == typeof(bool))
				{
					yield return new(typeof(SampleBooleanSpatialVariable));
				}
        var ReadValue = GetNodeForType(target, [
          new NodeTypeRecord(typeof(SampleValueSpatialVariable<>), null, null),
          new NodeTypeRecord(typeof(SampleObjectSpatialVariable<>), null, null),
        ]);
        yield return new(ReadValue);

        if (target.IsValueType)
				{	
          yield return new(typeof(SampleNumericSpatialVariable<>).MakeGenericType(target));

          yield return new(typeof(SampleMinMaxSpatialVariable<>).MakeGenericType(target));

          yield return new(typeof(SampleSpatialVariablePartialDerivative<>).MakeGenericType(target));
				}
      }
    }
  }
}