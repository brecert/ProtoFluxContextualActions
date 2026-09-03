using System.Collections.Frozen;

using ProtoFlux.Runtimes.Execution.Nodes;

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