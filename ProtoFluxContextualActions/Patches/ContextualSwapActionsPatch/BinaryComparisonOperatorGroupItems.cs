using System;
using System.Collections.Generic;
using System.Linq;
using Elements.Core;
using FrooxEngine;
using ProtoFlux.Runtimes.Execution.Nodes;
using ProtoFlux.Runtimes.Execution.Nodes.Operators;
using ProtoFluxContextualActions.Utils;
using HarmonyLib;
using ProtoFlux.Core;

namespace ProtoFluxContextualActions.Patches;

static partial class ContextualSwapActionsPatch
{
  static readonly HashSet<Type> ValueComparisonBinaryOperatorGroup = [
    typeof(ValueEquals<>),
    typeof(ValueNotEquals<>),
    typeof(ValueLessThan<>),
    typeof(ValueLessOrEqual<>),
    typeof(ValueGreaterThan<>),
    typeof(ValueGreaterOrEqual<>),
  ];


  static readonly HashSet<Type> ObjectComparisonBinaryOperatorGroup = [
    typeof(ObjectEquals<>),
    typeof(ObjectNotEquals<>),
    typeof(ObjectLessThan<>),
    typeof(ObjectLessOrEqual<>),
    typeof(ObjectGreaterThan<>),
    typeof(ObjectGreaterOrEqual<>),
  ];

  internal static IEnumerable<MenuItem> BinaryComparisonOperatorGroupItems(ContextualContext context)
  {
    var psuedoGenericTypes = context.World.GetPsuedoGenericTypesForWorld();

    if (context.NodeType.TryGetGenericTypeDefinition(out var genericTypeDefinition))
    {
      var pseudoGenericComparisonGroup = psuedoGenericTypes.ComparisonNodes().ToDictionary();

      if (ValueComparisonBinaryOperatorGroup.Contains(genericTypeDefinition))
      {
        foreach (var genericMatch in ValueComparisonBinaryOperatorGroup)
        {
          if (genericMatch.TryMakeGenericType(context.NodeType.GenericTypeArguments) is Type validType)
          {
            yield return new(validType);
          }
        }

        foreach (var psuedoGenericMatch in pseudoGenericComparisonGroup.Where(n => n.Value.SequenceEqual(context.NodeType.GenericTypeArguments)).Select(a => a.Key))
        {
          yield return new(psuedoGenericMatch);
        }
      }
      else if (ObjectComparisonBinaryOperatorGroup.Contains(genericTypeDefinition))
      {
        foreach (var genericMatch in ObjectComparisonBinaryOperatorGroup)
        {
          if (genericMatch.TryMakeGenericType(context.NodeType.GenericTypeArguments) is Type validType)
          {
            yield return new(validType);
          }
        }

        foreach (var psuedoGenericMatch in pseudoGenericComparisonGroup.Where(n => n.Value.SequenceEqual(context.NodeType.GenericTypeArguments)).Select(a => a.Key))
        {
          yield return new(psuedoGenericMatch);
        }
      }
    }
    else
    {
      var pseudoGenericComparisonGroup = psuedoGenericTypes.ComparisonNodes().ToDictionary();

      if (pseudoGenericComparisonGroup.TryGetValue(context.NodeType, out var genericArguments))
      {
        var psuedoGenericMatches = pseudoGenericComparisonGroup
          .Where(c => c.Value.SequenceEqual(genericArguments))
          .Select(c => c.Key);

        foreach (var match in psuedoGenericMatches)
        {
          yield return new(match);
        }

        var genericNodes = genericArguments.First().IsUnmanaged()
          ? ValueComparisonBinaryOperatorGroup
          : ObjectComparisonBinaryOperatorGroup;

        var genericMatches = genericNodes
          .Select(t => t.TryMakeGenericType([.. genericArguments]))
          .OfType<Type>()
          .Where(t => t.IsValidGenericType(validForInstantiation: false));

        foreach (var match in genericMatches)
        {
          yield return new(match);
        }
      }
    }
  }
}