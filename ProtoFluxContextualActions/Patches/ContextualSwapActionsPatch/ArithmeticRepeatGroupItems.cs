using System;
using System.Collections.Generic;
using Elements.Core;
using HarmonyLib;
using ProtoFlux.Runtimes.Execution.Nodes.Math;
using ProtoFlux.Runtimes.Execution.Nodes.Operators;
using ProtoFluxContextualActions.Utils;

namespace ProtoFluxContextualActions.Patches;

static partial class ContextualSwapActionsPatch
{
  static readonly HashSet<Type> ArithmeticRepeatGroup = [
    typeof(ValueMod<>),
    typeof(ValueRepeat<>),
  ];

  internal static IEnumerable<MenuItem> ArithmeticRepeatGroupItems(Type nodeType)
  {
    if (TypeUtils.TryGetGenericTypeDefinition(nodeType, out var genericType) && ArithmeticRepeatGroup.Contains(genericType))
    {
      var opType = nodeType.GenericTypeArguments[0];
      var coder = Traverse.Create(typeof(Coder<>).MakeGenericType(opType));

      static MenuItem RepeatItem(Type nodeType) => new(
        node: nodeType,
        connectionTransferType: ConnectionTransferType.ByIndexLossy
      );

      if (coder.Property<bool>("SupportsRepeat").Value)
      {
        yield return RepeatItem(typeof(ValueRepeat<>).MakeGenericType(opType));
      }

      if (coder.Property<bool>("SupportsMod").Value)
      {
        yield return RepeatItem(typeof(ValueMod<>).MakeGenericType(opType));
      }
    }
  }
}