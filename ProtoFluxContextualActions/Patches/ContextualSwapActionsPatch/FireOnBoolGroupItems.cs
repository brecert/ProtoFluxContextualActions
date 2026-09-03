using ProtoFlux.Runtimes.Execution.Nodes.Actions;

namespace ProtoFluxContextualActions.Patches;

static partial class ContextualSwapActionsPatch
{
  static readonly HashSet<Type> FireOnBoolGroup = [
    typeof(FireOnTrue),
    typeof(FireOnFalse),
    typeof(FireOnValueChange<bool>),
  ];

  internal static IEnumerable<MenuItem> FireOnBoolGroupItems(ContextualContext context)
  {
    if (FireOnBoolGroup.Contains(context.NodeType))
    {
      foreach (var match in FireOnBoolGroup)
      {
        yield return new MenuItem(match, connectionTransferType: ConnectionTransferType.ByIndexLossy);
      }
    }
  }
}