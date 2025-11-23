using System;
using System.Collections.Generic;
using System.Linq;
using Elements.Core;
using FrooxEngine.ProtoFlux;
using ProtoFlux.Runtimes.Execution;
using ProtoFlux.Runtimes.Execution.Nodes;
using ProtoFlux.Runtimes.Execution.Nodes.FrooxEngine.Variables;
using ProtoFluxContextualActions.Utils;

namespace ProtoFluxContextualActions.Patches;

static partial class ContextualSwapActionsPatch
{
  static readonly HashSet<Type> ValueWriteGroup = [
    typeof(ValueWrite<>),
    typeof(ValueWriteLatch<>),
  ];

  static readonly HashSet<Type> ValueWriteWithContextGroup = [
    typeof(ValueWrite<,>),
    typeof(ValueWriteLatch<,>),
  ];

  static readonly HashSet<Type> ObjectWriteGroup = [
    typeof(ObjectWrite<>),
    typeof(ObjectWriteLatch<>),
  ];

  static readonly HashSet<Type> ObjectWriteWithContextGroup = [
    typeof(ObjectWrite<,>),
    typeof(ObjectWriteLatch<,>),
  ];


  internal static IEnumerable<MenuItem> WriteGroupItems(ContextualContext context)
  {
    if (TypeUtils.TryGetGenericTypeDefinition(context.NodeType, out var genericType))
    {
      if (ValueWriteGroup.Contains(genericType))
      {
        foreach (var match in ValueWriteGroup)
        {
          yield return new MenuItem(match.MakeGenericType(context.NodeType.GenericTypeArguments));
        }
      }

      else if (ValueWriteWithContextGroup.Contains(genericType))
      {
        foreach (var match in ValueWriteWithContextGroup)
        {
          yield return new MenuItem(match.MakeGenericType(context.NodeType.GenericTypeArguments));
        }
      }

      else if (ObjectWriteGroup.Contains(genericType))
      {
        foreach (var match in ObjectWriteGroup)
        {
          yield return new MenuItem(match.MakeGenericType(context.NodeType.GenericTypeArguments));
        }
      }

      else if (ObjectWriteWithContextGroup.Contains(genericType))
      {
        foreach (var match in ObjectWriteWithContextGroup)
        {
          yield return new MenuItem(match.MakeGenericType(context.NodeType.GenericTypeArguments));
        }
      }
    }
  }
}