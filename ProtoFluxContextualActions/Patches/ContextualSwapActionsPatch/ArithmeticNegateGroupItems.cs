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


  internal static IEnumerable<MenuItem> ArithmeticNegateGroupItems(Type nodeType)
  {
    if (TypeUtils.TryGetGenericTypeDefinition(nodeType, out var genericType) && ArithmeticNegateGroup.Contains(genericType))
    {
      var opType = nodeType.GenericTypeArguments[0];
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