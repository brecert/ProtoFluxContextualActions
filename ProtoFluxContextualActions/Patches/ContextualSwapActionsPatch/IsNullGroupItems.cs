using System;
using System.Collections.Frozen;
using System.Collections.Generic;
using ProtoFlux.Runtimes.Execution.Nodes;
using ProtoFlux.Runtimes.Execution.Nodes.FrooxEngine.Interaction;
using ProtoFluxContextualActions.Utils;

namespace ProtoFluxContextualActions.Patches;

static partial class ContextualSwapActionsPatch
{
  static readonly FrozenSet<Type> IsNullGroupItems = [
    typeof(IsNull<>),
    typeof(NotNull<>),
  ];

  internal static IEnumerable<MenuItem> IsNullGroupItemsGroupItems(ContextualContext context) =>
    MatchGenericTypes(IsNullGroupItems, context.NodeType);
}