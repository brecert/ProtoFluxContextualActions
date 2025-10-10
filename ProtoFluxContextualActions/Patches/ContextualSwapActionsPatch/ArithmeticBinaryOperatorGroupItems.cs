using System;
using System.Collections.Generic;
using Elements.Core;
using HarmonyLib;
using ProtoFlux.Runtimes.Execution.Nodes.Operators;
using ProtoFluxContextualActions.Utils;

namespace ProtoFluxContextualActions.Patches;

static partial class ContextualSwapActionsPatch
{
  static readonly HashSet<Type> ArithmeticBinaryOperatorGroup = [
    typeof(ValueAdd<>),
    typeof(ValueSub<>),
    typeof(ValueMul<>),
    typeof(ValueDiv<>),
    typeof(ValueMod<>),
  ];

  internal static IEnumerable<MenuItem> ArithmeticBinaryOperatorGroupItems(ContextualContext context)
  {
    if (TypeUtils.TryGetGenericTypeDefinition(context.NodeType, out var genericType) && ArithmeticBinaryOperatorGroup.Contains(genericType))
    {
      var opType = context.NodeType.GenericTypeArguments[0];
      var coder = Traverse.Create(typeof(Coder<>).MakeGenericType(opType));

      if (coder.Property<bool>("SupportsAddSub").Value)
      {
        yield return new MenuItem(typeof(ValueAdd<>).MakeGenericType(opType));
        yield return new MenuItem(typeof(ValueSub<>).MakeGenericType(opType));
      }

      if (coder.Property<bool>("SupportsMul").Value)
      {
        yield return new MenuItem(typeof(ValueMul<>).MakeGenericType(opType));
      }

      if (coder.Property<bool>("SupportsDiv").Value)
      {
        yield return new MenuItem(typeof(ValueDiv<>).MakeGenericType(opType));
      }

      if (coder.Property<bool>("SupportsMod").Value)
      {
        yield return new MenuItem(typeof(ValueMod<>).MakeGenericType(opType));
      }
    }
  }

}