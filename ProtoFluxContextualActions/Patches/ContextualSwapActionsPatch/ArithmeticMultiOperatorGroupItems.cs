using System;
using System.Collections.Generic;
using Elements.Core;
using HarmonyLib;
using ProtoFlux.Core;
using ProtoFlux.Runtimes.Execution.Nodes.Operators;
using ProtoFluxContextualActions.Utils;

namespace ProtoFluxContextualActions.Patches;

static partial class ContextualSwapActionsPatch
{
  static readonly HashSet<Type> ArithmeticMultiOperatorGroup = [
    typeof(ValueAddMulti<>),
    typeof(ValueSubMulti<>),
    typeof(ValueMulMulti<>),
    typeof(ValueDivMulti<>),
  ];

  internal static IEnumerable<MenuItem> ArithmeticMultiOperatorGroupItems(Type nodeType)
  {
    if (TypeUtils.TryGetGenericTypeDefinition(nodeType, out var genericType) && ArithmeticMultiOperatorGroup.Contains(genericType))
    {
      var opType = nodeType.GenericTypeArguments[0];
      var coder = Traverse.Create(typeof(Coder<>).MakeGenericType(opType));

      static MenuItem MultiMenuItem(Type nodeType) => new(
        node: nodeType,
        name: nodeType.GetNiceTypeName(),
        connectionTransferType: ConnectionTransferType.ByIndexLossy
      );

      if (coder.Property<bool>("SupportsAddSub").Value)
      {
        yield return MultiMenuItem(typeof(ValueAddMulti<>).MakeGenericType(opType));
        yield return MultiMenuItem(typeof(ValueSubMulti<>).MakeGenericType(opType));
      }

      if (coder.Property<bool>("SupportsMul").Value)
      {
        yield return MultiMenuItem(typeof(ValueMulMulti<>).MakeGenericType(opType));
      }

      if (coder.Property<bool>("SupportsDiv").Value)
      {
        yield return MultiMenuItem(typeof(ValueDivMulti<>).MakeGenericType(opType));
      }
    }
  }
}