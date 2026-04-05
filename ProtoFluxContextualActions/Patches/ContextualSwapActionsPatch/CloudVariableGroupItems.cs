using Elements.Core;
using FrooxEngine.ProtoFlux;
using ProtoFlux.Core;
using ProtoFlux.Runtimes.Execution.Nodes.Actions;
using ProtoFlux.Runtimes.Execution.Nodes.FrooxEngine.Cloud;
using ProtoFlux.Runtimes.Execution.Nodes.FrooxEngine.Variables;
using System;
using System.Collections.Generic;
using System.Linq;

namespace ProtoFluxContextualActions.Patches;

static partial class ContextualSwapActionsPatch
{
  static readonly HashSet<Type> CloudVariableGroup = [
    typeof(DynamicVariableValueInput<>),
    typeof(DynamicVariableObjectInput<>),

    typeof(ReadDynamicValueVariable<>),
    typeof(ReadDynamicObjectVariable<>),

    typeof(WriteDynamicValueVariable<>),
    typeof(WriteDynamicObjectVariable<>),

    typeof(CreateDynamicValueVariable<>),
    typeof(CreateDynamicObjectVariable<>),

    typeof(WriteOrCreateDynamicValueVariable<>),
    typeof(WriteOrCreateDynamicObjectVariable<>),
  ];


  internal static IEnumerable<MenuItem> CloudVariableGroupItems(ContextualContext context)
  {
    if (CloudVariableGroup.Any(t => context.NodeType.IsGenericType ? t == context.NodeType.GetGenericTypeDefinition() : t == context.NodeType))
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
      if (context.NodeType.IsGenericType && target == null)
      {
        var opCount = context.NodeType.GenericTypeArguments.Length;
        var opType = context.NodeType.GenericTypeArguments[opCount - 1];
        target = opType;
      }

      if (target != null)
      {
        var ReadCloud = GetNodeForType(target, [
          new NodeTypeRecord(typeof(ReadValueCloudVariable<>), null, null),
          new NodeTypeRecord(typeof(ReadObjectCloudVariable<>), null, null),
        ]);
        yield return new(ReadCloud);

        var WriteCloud = GetNodeForType(target, [
          new NodeTypeRecord(typeof(WriteValueCloudVariable<>), null, null),
          new NodeTypeRecord(typeof(WriteObjectCloudVariable<>), null, null),
        ]);
        yield return new(WriteCloud);
      }
    }
  }
}