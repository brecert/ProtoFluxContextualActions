using System;
using System.Collections.Generic;

using Elements.Core;

using ProtoFlux.Runtimes.Execution.Nodes;
using ProtoFlux.Runtimes.Execution.Nodes.FrooxEngine.Slots;

using ProtoFluxContextualActions.Utils;

namespace ProtoFluxContextualActions.Patches;

static partial class ContextualSwapActionsPatch
{
  static readonly HashSet<Type> ForEachWithIndexGroup = [
    typeof(ForEachWithIndexObject<,>),
    typeof(AsyncForEachWithIndexObject<,>),
    typeof(ReadOnlyForEachWithIndexObject<,>),
    typeof(ForEachWithIndexValue<,>),
    typeof(AsyncForEachWithIndexValue<,>),
    typeof(ReadOnlyForEachWithIndexValue<,>),
  ];

  static readonly HashSet<Type> ForEachObjectWithIndexGroup = [
    typeof(ForEachObject<,>),
    typeof(AsyncForEachObject<,>),
    typeof(ForEachWithIndexObject<,>),
    typeof(AsyncForEachWithIndexObject<,>),
    typeof(ReadOnlyForEachWithIndexObject<,>),
  ];

  static readonly HashSet<Type> ForEachValueWithIndexGroup = [
    typeof(ForEachValue<,>),
    typeof(AsyncForEachValue<,>),
    typeof(ForEachWithIndexValue<,>),
    typeof(AsyncForEachWithIndexValue<,>),
    typeof(ReadOnlyForEachWithIndexValue<,>),
  ];

  static readonly HashSet<Type> AsyncForEachGroup = [
    typeof(AsyncForEachValue<,>),
    typeof(AsyncForEachObject<,>),
    typeof(AsyncForEachWithIndexValue<,>),
    typeof(AsyncForEachWithIndexObject<,>),
  ];

  static readonly HashSet<Type> ReadOnlyForEachGroup = [
    typeof(ReadOnlyForEachWithIndexValue<,>),
    typeof(ReadOnlyForEachWithIndexObject<,>),
  ];

  internal static IEnumerable<MenuItem> ForEachGroupItems(ContextualContext context)
  {
    if (context.NodeType.TryGetGenericTypeDefinition(out var genericType))
    {
      if (ForEachObjectWithIndexGroup.Contains(genericType))
      {
        foreach (var type in ForEachObjectWithIndexGroup)
        {
          if (type.TryMakingGenericTypeFrom(context.NodeType) is { } filledType)
          {
            yield return new(filledType, name: NiceForEachName(type));
          }
        }
      }
      else if (ForEachValueWithIndexGroup.Contains(genericType))
      {
        foreach (var type in ForEachValueWithIndexGroup)
        {
          UniLog.Log(type);
          if (type.TryMakingGenericTypeFrom(context.NodeType) is { } filledType)
          {
            UniLog.Log(filledType);
            yield return new(filledType, name: NiceForEachName(type));
          }
        }
      }
    }
  }

  static string NiceForEachName(Type type) =>
    $"{(AsyncForEachGroup.Contains(type) ? "(Async) " : "")}{(ReadOnlyForEachGroup.Contains(type) ? "(ReadOnly) " : "")}ForEach{(ForEachWithIndexGroup.Contains(type) ? "WithIndex" : "")}";
}
