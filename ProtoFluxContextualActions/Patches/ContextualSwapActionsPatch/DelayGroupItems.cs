using System;
using System.Collections.Generic;
using System.Linq;
using FrooxEngine.ProtoFlux;
using ProtoFlux.Runtimes.Execution.Nodes;
using ProtoFlux.Runtimes.Execution.Nodes.Actions;
using ProtoFlux.Runtimes.Execution.Nodes.FrooxEngine.Async;
using ProtoFlux.Runtimes.Execution.Nodes.TimeAndDate;

namespace ProtoFluxContextualActions.Patches;

static partial class ContextualSwapActionsPatch
{
  static readonly HashSet<Type> DelayGroup = [
    typeof(DelaySecondsFloat),
    typeof(DelayUpdates),
    typeof(DelayUpdatesOrSecondsFloat),
    typeof(DelayTimeSpan),
  ];

  static readonly HashSet<Type> DelayDataGroup = [
    typeof(DelayWithValueSecondsFloat<>),
    typeof(DelayUpdatesWithValue<>),
    typeof(DelayUpdatesOrTimeWithValueSecondsFloat<>),
    typeof(DelayWithValueTimeSpan<>),


    typeof(DelayWithObjectSecondsFloat<>),
    typeof(DelayUpdatesWithObject<>),
    typeof(DelayUpdatesOrTimeWithObjectSecondsFloat<>),
    typeof(DelayWithObjectTimeSpan<>),
  ];

  internal static IEnumerable<MenuItem> DelayGroupItems(ContextualContext context)
  {
    if (DelayGroup.Contains(context.NodeType))
    {
      foreach (var match in DelayGroup)
      {
        yield return new MenuItem(match);
      }
    }

    if (DelayDataGroup.Any(t => context.NodeType.IsGenericType ? t == context.NodeType.GetGenericTypeDefinition() : t == context.NodeType))
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
        var delaySeconds = GetNodeForType(target, [
          new NodeTypeRecord(typeof(DelayWithValueSecondsFloat<>), null, null),
          new NodeTypeRecord(typeof(DelayWithObjectSecondsFloat<>), null, null),
        ]);
        yield return new(delaySeconds);

        var delayUpdates = GetNodeForType(target, [
          new NodeTypeRecord(typeof(DelayUpdatesWithValue<>), null, null),
          new NodeTypeRecord(typeof(DelayUpdatesWithObject<>), null, null),
        ]);
        yield return new(delayUpdates);

        var delayUpdatesOrSeconds = GetNodeForType(target, [
          new NodeTypeRecord(typeof(DelayUpdatesOrTimeWithValueSecondsFloat<>), null, null),
          new NodeTypeRecord(typeof(DelayUpdatesOrTimeWithObjectSecondsFloat<>), null, null),
        ]);
        yield return new(delayUpdatesOrSeconds);

        var delayTimespan = GetNodeForType(target, [
          new NodeTypeRecord(typeof(DelayWithValueTimeSpan<>), null, null),
          new NodeTypeRecord(typeof(DelayWithObjectTimeSpan<>), null, null),
        ]);
        yield return new(delayTimespan);
      }
    }
  }
}