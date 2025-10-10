using System;
using System.Collections.Generic;
using Elements.Core;
using ProtoFlux.Core;
using ProtoFlux.Runtimes.Execution.Nodes.Math;
using ProtoFlux.Runtimes.Execution.Nodes.Operators;
using ProtoFluxContextualActions.Utils;

namespace ProtoFluxContextualActions.Patches;

static partial class ContextualSwapActionsPatch
{
  static readonly BiDictionary<Type, Type> MultiInputMappingGroup = new()
  {
    {typeof(ValueAdd<>), typeof(ValueAddMulti<>)},
    {typeof(ValueSub<>), typeof(ValueSubMulti<>)},
    {typeof(ValueMul<>), typeof(ValueMulMulti<>)},
    {typeof(ValueDiv<>), typeof(ValueDivMulti<>)},
    {typeof(ValueMin<>), typeof(ValueMinMulti<>)},
    {typeof(ValueMax<>), typeof(ValueMaxMulti<>)},
  };

  internal static IEnumerable<MenuItem> MultiInputMappingGroupItems(Type nodeType)
  {
    if (TypeUtils.TryGetGenericTypeDefinition(nodeType, out var genericType))
    {
      if (MultiInputMappingGroup.TryGetSecond(genericType, out var mapped))
      {
        var binopType = nodeType.GenericTypeArguments[0];
        yield return new MenuItem(
          node: mapped.MakeGenericType(binopType),
          name: mapped.GetNiceTypeName(),
          connectionTransferType: ConnectionTransferType.ByIndexLossy
        );
      }
      else if (MultiInputMappingGroup.TryGetFirst(genericType, out mapped))
      {
        var binopType = nodeType.GenericTypeArguments[0];
        yield return new MenuItem(mapped.MakeGenericType(binopType), connectionTransferType: ConnectionTransferType.ByIndexLossy);
      }
    }
  }
}