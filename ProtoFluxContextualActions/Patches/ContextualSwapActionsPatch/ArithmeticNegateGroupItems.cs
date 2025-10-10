using System;
using System.Collections.Generic;
using Elements.Core;
using HarmonyLib;
using ProtoFlux.Runtimes.Execution.Nodes.Operators;
using ProtoFluxContextualActions.Utils;

namespace ProtoFluxContextualActions.Patches;

static partial class ContextualSwapActionsPatch
{
  static readonly HashSet<Type> ArithmeticNegateGroup = [
    typeof(ValueNegate<>),
    typeof(ValuePlusMinus<>),
  ];


  internal static IEnumerable<MenuItem> ArithmeticNegateGroupItems(ContextualContext context)
  {
    if (TypeUtils.TryGetGenericTypeDefinition(context.NodeType, out var genericType) && ArithmeticNegateGroup.Contains(genericType))
    {
      var opType = context.NodeType.GenericTypeArguments[0];
      var coder = Traverse.Create(typeof(Coder<>).MakeGenericType(opType));

      if (coder.Property<bool>("SupportsNegate").Value)
      {
        yield return new(typeof(ValueNegate<>).MakeGenericType(opType));
      }

      if (coder.Property<bool>("SupportsAddSub").Value)
      {
        yield return new(typeof(ValuePlusMinus<>).MakeGenericType(opType));
      }
    }
  }
}