using System;
using System.Collections.Generic;
using System.Linq;
using Elements.Core;
using HarmonyLib;
using ProtoFlux.Runtimes.Execution.Nodes.Actions;
using ProtoFlux.Runtimes.Execution.Nodes.Operators;
using ProtoFluxContextualActions.Utils;

namespace ProtoFluxContextualActions.Patches;

static partial class ContextualSwapActionsPatch
{
  static readonly HashSet<Type> ArithmeticOneGroup = [
    typeof(ValueInc<>),
    typeof(ValueDec<>),
    typeof(ValueIncrement<>),
    typeof(ValueDecrement<>),
    typeof(ValueIncrement<,>),
    typeof(ValueDecrement<,>),
  ];

  internal static IEnumerable<MenuItem> ArithmeticOneGroupItems(ContextualContext context)
  {
    if (TypeUtils.TryGetGenericTypeDefinition(context.NodeType, out var genericType) && ArithmeticOneGroup.Contains(genericType))
    {
      var opCount = context.NodeType.GenericTypeArguments.Length;
      var opType = context.NodeType.GenericTypeArguments[opCount - 1];
      var coder = Traverse.Create(typeof(Coder<>).MakeGenericType(opType));

      // in theory, this check shouldn't be needed
      // in practice, https://github.com/Yellow-Dog-Man/Resonite-Issues/issues/3319
      if (coder.Property<bool>("SupportsAddSub").Value)
      {
        yield return new(typeof(ValueInc<>).MakeGenericType(opType));
        yield return new(typeof(ValueDec<>).MakeGenericType(opType));
        yield return new(typeof(ValueIncrement<>).MakeGenericType(opType));
        yield return new(typeof(ValueDecrement<>).MakeGenericType(opType));
      }
    }
  }
}