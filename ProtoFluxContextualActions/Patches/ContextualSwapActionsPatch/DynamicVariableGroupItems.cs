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
  static readonly HashSet<Type> DynamicVariableGroup = [
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

    typeof(DeleteDynamicVariable<>),
    typeof(ClearDynamicVariablesOfType<>),
  ];


  internal static IEnumerable<MenuItem> DynamicVariableGroupItems(ContextualContext context)
  {
    if (DynamicVariableGroup.Any(t => context.NodeType.IsGenericType ? t == context.NodeType.GetGenericTypeDefinition() : t == context.NodeType))
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
        var ReadDyn = GetNodeForType(target, [
          new NodeTypeRecord(typeof(ReadDynamicValueVariable<>), null, null),
          new NodeTypeRecord(typeof(ReadDynamicObjectVariable<>), null, null),
        ]);
        yield return new(ReadDyn);

        var WriteDyn = GetNodeForType(target, [
          new NodeTypeRecord(typeof(WriteDynamicValueVariable<>), null, null),
          new NodeTypeRecord(typeof(WriteDynamicObjectVariable<>), null, null),
        ]);
        yield return new(WriteDyn);

        var DynInput = GetNodeForType(target, [
          new NodeTypeRecord(typeof(DynamicVariableValueInput<>), null, null),
          new NodeTypeRecord(typeof(DynamicVariableObjectInput<>), null, null),
        ]);
        yield return new(DynInput);

        var CreateDyn = GetNodeForType(target, [
          new NodeTypeRecord(typeof(CreateDynamicValueVariable<>), null, null),
          new NodeTypeRecord(typeof(CreateDynamicObjectVariable<>), null, null),
        ]);
        yield return new(CreateDyn);

        var WriteOrCreateDyn = GetNodeForType(target, [
          new NodeTypeRecord(typeof(WriteOrCreateDynamicValueVariable<>), null, null),
          new NodeTypeRecord(typeof(WriteOrCreateDynamicObjectVariable<>), null, null),
        ]);
        yield return new(WriteOrCreateDyn);

        yield return new(typeof(DeleteDynamicVariable<>));
        yield return new(typeof(ClearDynamicVariablesOfType<>));
      }
    }
  }
}