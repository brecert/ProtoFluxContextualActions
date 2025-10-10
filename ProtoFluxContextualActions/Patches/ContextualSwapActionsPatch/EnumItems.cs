using System;
using System.Collections.Generic;
using System.Linq;
using Elements.Core;
using ProtoFlux.Runtimes.Execution.Nodes.Enums;
using ProtoFluxContextualActions.Extensions;
using ProtoFluxContextualActions.Utils;

namespace ProtoFluxContextualActions.Patches;

static partial class ContextualSwapActionsPatch
{
  static readonly HashSet<Type> EnumShiftGroup = [
    typeof(NextValue<>),
    typeof(PreviousValue<>),
    typeof(ShiftEnum<>),
  ];

  static readonly BiDictionary<Type, Type> EnumToNumberGroup =
    NodeUtils.EnumToNumberTypeMap.Values.Zip(NodeUtils.TryEnumToNumberTypeMap.Values).ToBiDictionary();

  static readonly BiDictionary<Type, Type> NumberToEnumGroup =
    NodeUtils.NumberToEnumTypeMap.Values.Zip(NodeUtils.TryNumberToEnumTypeMap.Values).ToBiDictionary();

  internal static IEnumerable<MenuItem> EnumShiftGroupItems(ContextualContext context)
  {
    if (TypeUtils.TryGetGenericTypeDefinition(context.NodeType, out var genericType) && EnumShiftGroup.Contains(genericType))
    {
      foreach (var match in EnumShiftGroup)
      {
        yield return new MenuItem(match.MakeGenericType(context.NodeType.GenericTypeArguments[0]), name: match.GetNiceName());
      }
    }
  }

  internal static IEnumerable<MenuItem> NumberToEnumGroupItems(ContextualContext context)
  {
    if (TypeUtils.TryGetGenericTypeDefinition(context.NodeType, out var genericType) && TryGetSwap(NumberToEnumGroup, genericType, out var match))
    {
      var enumType = context.NodeType.GenericTypeArguments[0];
      yield return new MenuItem(match.MakeGenericType(enumType));
    }
  }

  internal static IEnumerable<MenuItem> EnumToNumberGroupItems(ContextualContext context)
  {
    if (TypeUtils.TryGetGenericTypeDefinition(context.NodeType, out var genericType) && TryGetSwap(EnumToNumberGroup, genericType, out var match))
    {
      var enumType = context.NodeType.GenericTypeArguments[0];
      yield return new(match.MakeGenericType(enumType));
    }
  }
}