using System;
using System.Collections.Generic;
using System.Linq;
using Elements.Core;
using FrooxEngine.ProtoFlux.Runtimes.Execution.Nodes.Math.Constants;
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

  static readonly HashSet<Type> ArithmeticAddOneGroup = [
    typeof(ValueAdd<>),
    typeof(ValueInc<>),
  ];

  static readonly HashSet<Type> ArithmeticSubOneGroup = [
    typeof(ValueSub<>),
    typeof(ValueDec<>),
    typeof(ValueOneMinus<>),
    typeof(ValueNegate<>),
    typeof(ValueReciprocal<>),
  ];

  static readonly HashSet<Type> ArithmeticSquareGroup = [
    typeof(ValueSquare<>),
    typeof(ValueCube<>)
  ];

  internal static IEnumerable<MenuItem> ArithmeticOneGroupItems(ContextualContext context)
  {
    var psuedoGenerics = context.World.GetPsuedoGenericTypesForWorld();
    if (TypeUtils.TryGetGenericTypeDefinition(context.NodeType, out var genericType))
    {
      var opCount = context.NodeType.GenericTypeArguments.Length;
      var opType = context.NodeType.GenericTypeArguments[opCount - 1];
      var coder = Traverse.Create(typeof(Coder<>).MakeGenericType(opType));

      // in theory, this check shouldn't be needed
      // in practice, https://github.com/Yellow-Dog-Man/Resonite-Issues/issues/3319
      if (coder.Property<bool>("SupportsAddSub").Value)
      {
        if (ArithmeticOneGroup.Contains(genericType))
        {
          yield return new(typeof(ValueInc<>).MakeGenericType(opType));
          yield return new(typeof(ValueDec<>).MakeGenericType(opType));
          yield return new(typeof(ValueIncrement<>).MakeGenericType(opType));
          yield return new(typeof(ValueDecrement<>).MakeGenericType(opType));
        }

        if (ArithmeticAddOneGroup.Contains(genericType))
        {
          foreach (var match in ArithmeticAddOneGroup)
          {
            yield return new(match.MakeGenericType(opType), connectionTransferType: ConnectionTransferType.ByIndexLossy);
          }
        }

        if (ArithmeticSubOneGroup.Contains(genericType))
        {
          foreach (var match in ArithmeticSubOneGroup)
          {
            yield return new(match.MakeGenericType(opType), connectionTransferType: ConnectionTransferType.ByIndexLossy);
          }
        }

        if (ArithmeticSquareGroup.Contains(genericType))
        {
          foreach (var match in ArithmeticSquareGroup)
          {
            yield return new(match.MakeGenericType(opType), connectionTransferType: ConnectionTransferType.ByIndexLossy);
          }
          if (psuedoGenerics.Sqrt.Any(t => t.Types.First() == opType))
          {
            yield return new(psuedoGenerics.Sqrt.First(t => t.Types.First() == opType).Node);
          }
          if (psuedoGenerics.NthRoot.Any(t => t.Types.First() == opType))
          {
            yield return new(psuedoGenerics.NthRoot.First(t => t.Types.First() == opType).Node);
          }
        }
      }
    }

    Type? psuedoGenericType = null;
    if (psuedoGenerics.Sqrt.Any(t => t.Node == context.NodeType))
    {
      psuedoGenericType = psuedoGenerics.Sqrt.First(t => t.Node == context.NodeType).Types.First();
    }
    if (psuedoGenerics.NthRoot.Any(t => t.Node == context.NodeType))
    {
      psuedoGenericType = psuedoGenerics.NthRoot.First(t => t.Node == context.NodeType).Types.First();
    }

    if (psuedoGenericType != null)
    {
      foreach (var match in ArithmeticSquareGroup)
      {
        yield return new(match.MakeGenericType(psuedoGenericType), connectionTransferType: ConnectionTransferType.ByIndexLossy);
      }
      if (psuedoGenerics.Sqrt.Any(t => t.Types.First() == psuedoGenericType))
      {
        yield return new(psuedoGenerics.Sqrt.First(t => t.Types.First() == psuedoGenericType).Node);
      }
      if (psuedoGenerics.NthRoot.Any(t => t.Types.First() == psuedoGenericType))
      {
        yield return new(psuedoGenerics.NthRoot.First(t => t.Types.First() == psuedoGenericType).Node);
      }
    }
  }
}