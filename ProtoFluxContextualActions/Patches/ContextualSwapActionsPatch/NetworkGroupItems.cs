using ProtoFlux.Runtimes.Execution.Nodes.FrooxEngine.Network;

namespace ProtoFluxContextualActions.Patches;

static partial class ContextualSwapActionsPatch
{
  static readonly HashSet<Type> NetworkGroup = [
    typeof(GET_String),
    typeof(POST_String),
  ];
  internal static IEnumerable<MenuItem> NetworkGroupItems(ContextualContext context)
  {
    if (NetworkGroup.Contains(context.NodeType))
    {
      foreach (var match in NetworkGroup)
      {
        yield return new MenuItem(match);
      }
    }
  }
}