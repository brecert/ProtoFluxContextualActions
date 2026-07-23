using System;
using System.Collections.Frozen;
using System.Collections.Generic;
using ProtoFlux.Runtimes.Execution.Nodes.FrooxEngine.Slots;

namespace ProtoFluxContextualActions.Patches;

static partial class ContextualSwapActionsPatch
{
  static readonly FrozenSet<Type> SlotChildGroup = [
    typeof(GetChild),
    typeof(GetObjectRoot),
    typeof(GetParentSlot),
    typeof(IsChildOf), // doesn't really fit with the rest but the issue requests this.
    typeof(SetParent), // this is stupid (but it allows GetParent<->SetParent so its ok)
  ];

  internal static IEnumerable<MenuItem> SlotChildGroupItems(ContextualContext context) =>
    MatchNonGenericTypes(SlotChildGroup, context.NodeType);
}