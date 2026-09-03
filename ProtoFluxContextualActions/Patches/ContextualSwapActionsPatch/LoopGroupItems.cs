using ProtoFlux.Runtimes.Execution.Nodes;

namespace ProtoFluxContextualActions.Patches;

static partial class ContextualSwapActionsPatch
{
  static readonly HashSet<Type> LoopGroup = [
    typeof(For),
    typeof(While),
    typeof(RangeLoopInt),
    typeof(AsyncFor),
    typeof(AsyncWhile),
    typeof(AsyncRangeLoopInt),
  ];

  internal static IEnumerable<MenuItem> LoopGroupItems(ContextualContext context)
  {
    if (LoopGroup.Contains(context.NodeType))
    {
      foreach (var match in LoopGroup)
      {
        yield return new MenuItem(match);
      }
    }
  }
}