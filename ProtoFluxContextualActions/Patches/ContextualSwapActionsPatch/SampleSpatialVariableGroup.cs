using System;
using System.Collections.Frozen;
using System.Collections.Generic;
using HarmonyLib;
using ProtoFlux.Runtimes.Execution.Nodes.FrooxEngine.Variables;
using ProtoFluxContextualActions.Utils;

namespace ProtoFluxContextualActions.Patches;

static partial class ContextualSwapActionsPatch
{
  static readonly FrozenSet<Type> SampleSpatialVariableGroup = [
    // typeof(SampleBooleanSpatialVariable),
    typeof(SampleValueSpatialVariable<>),
    typeof(SampleMinMaxSpatialVariable<>),
    typeof(SampleNumericSpatialVariable<>),
    typeof(SampleSpatialVariablePartialDerivative<>),
  ];

  internal static IEnumerable<MenuItem> SampleSpatialVariableGroupItems(ContextualContext context)
  {
    var nodeType = context.NodeType;
    if (nodeType.TryGetGenericTypeDefinition(out var genericType) && SampleSpatialVariableGroup.Contains(genericType))
    {
      foreach (var match in SampleSpatialVariableGroup)
      {
        var type = match.MakeGenericType(nodeType.GenericTypeArguments);
        if (IsValidGenericType(type))
        {
          // TODO: have proper transfer types, this having it be lossy is fine for now but not ideal.
          yield return new MenuItem(match.MakeGenericType(nodeType.GenericTypeArguments));
        }
      }

      if (nodeType.GenericTypeArguments[0] == typeof(bool))
      {
        yield return new MenuItem(typeof(SampleBooleanSpatialVariable));
      }
    }
    else if (nodeType == typeof(SampleBooleanSpatialVariable))
    {
      foreach (var match in SampleSpatialVariableGroup)
      {
        var type = match.MakeGenericType(typeof(bool));
        if (IsValidGenericType(type))
        {
          yield return new MenuItem(type);
        }
      }
    }
  }

  static bool IsValidGenericType(Type type) => Traverse.Create(type).Property<bool>("IsValidGenericType").Value;
}