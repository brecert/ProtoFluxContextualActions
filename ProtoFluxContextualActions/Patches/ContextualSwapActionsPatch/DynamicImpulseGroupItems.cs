using System;
using System.Collections.Frozen;
using System.Collections.Generic;
using Microsoft.VisualBasic;
using ProtoFlux.Runtimes.Execution.Nodes.Actions;
using ProtoFluxContextualActions.Utils;

namespace ProtoFluxContextualActions.Patches;

static partial class ContextualSwapActionsPatch
{
  public static readonly FrozenSet<Type> DynamicImpulseGroup = [
    typeof(DynamicImpulseTrigger),
    typeof(DynamicImpulseReceiver),
  ];

  public static readonly FrozenSet<Type> AsyncDynamicImpulseGroup = [
    typeof(AsyncDynamicImpulseTrigger),
    typeof(AsyncDynamicImpulseReceiver),
  ];

  public static readonly FrozenSet<Type> DynamicImpulseValueGroup = [
    typeof(DynamicImpulseTriggerWithValue<>),
    typeof(DynamicImpulseReceiverWithValue<>),
  ];

  public static readonly FrozenSet<Type> AsyncDynamicImpulseValueGroup = [
    typeof(AsyncDynamicImpulseTriggerWithValue<>),
    typeof(AsyncDynamicImpulseReceiverWithValue<>),
  ];

  public static readonly FrozenSet<Type> DynamicImpulseObjectGroup = [
    typeof(DynamicImpulseTriggerWithObject<>),
    typeof(DynamicImpulseReceiverWithObject<>),
  ];

  public static readonly FrozenSet<Type> AsyncDynamicImpulseObjectGroup = [
    typeof(AsyncDynamicImpulseTriggerWithObject<>),
    typeof(AsyncDynamicImpulseReceiverWithObject<>),
  ];

  public static readonly IEnumerable<FrozenSet<Type>> ImpulseGroups = [
    DynamicImpulseGroup,
    AsyncDynamicImpulseGroup
  ];

  public static readonly IEnumerable<FrozenSet<Type>> ImpulseGroupsWithData = [
    DynamicImpulseValueGroup,
    AsyncDynamicImpulseValueGroup,
    DynamicImpulseObjectGroup,
    AsyncDynamicImpulseObjectGroup
  ];


  internal static IEnumerable<MenuItem> DynamicImpulseGroupItems(ContextualContext context)
  {
    if (context.NodeType.TryGetGenericTypeDefinition(out var genericType))
    {
      foreach (var group in ImpulseGroupsWithData)
      {
        if (group.Contains(genericType))
        {
          foreach (var match in group)
          {
            yield return new(match.MakeGenericType(context.NodeType.GenericTypeArguments));
          }
        }
      }
    }
    else
    {
      foreach (var group in ImpulseGroups)
      {
        if (group.Contains(context.NodeType))
        {
          foreach (var match in group)
          {
            yield return new(match);
          }
        }
      }
    }
  }
}