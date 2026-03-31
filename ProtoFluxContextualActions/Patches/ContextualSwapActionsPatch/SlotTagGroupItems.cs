using System;
using System.Collections.Frozen;
using System.Collections.Generic;
using ProtoFlux.Runtimes.Execution.Nodes.FrooxEngine.Slots;

namespace ProtoFluxContextualActions.Patches;

static partial class ContextualSwapActionsPatch
{
  static readonly FrozenSet<Type> SlotTagGroup = [
    typeof(GetTag),
    typeof(HasTag),
  ];

  internal static IEnumerable<MenuItem> SlotTagGroupItems(ContextualContext context) =>
    MatchNonGenericTypes(SlotTagGroup, context.NodeType);
}