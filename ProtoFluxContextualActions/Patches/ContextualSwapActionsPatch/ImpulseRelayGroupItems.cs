using ProtoFlux.Runtimes.Execution.Nodes;

namespace ProtoFluxContextualActions.Patches;

static partial class ContextualSwapActionsPatch
{
  static readonly HashSet<Type> ImpulseRelayGroup = [
    typeof(CallRelay),
    typeof(ContinuationRelay),
    typeof(AsyncCallRelay),
  ];

  internal static IEnumerable<MenuItem> ImpulseRelayGroupItems(ContextualContext context)
  {
    if (ImpulseRelayGroup.Contains(context.NodeType))
    {
      foreach (var match in ImpulseRelayGroup)
      {
        yield return new MenuItem(match);
      }
    }
  }
}