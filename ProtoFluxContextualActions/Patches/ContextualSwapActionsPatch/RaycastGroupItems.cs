using ProtoFlux.Runtimes.Execution.Nodes.FrooxEngine.Physics;

namespace ProtoFluxContextualActions.Patches;

static partial class ContextualSwapActionsPatch
{
  static readonly HashSet<Type> RaycastGroup = [
    typeof(Raycaster),
    typeof(RaycastOne),
  ];
  internal static IEnumerable<MenuItem> RaycastGroupItems(ContextualContext context)
  {
    if (RaycastGroup.Contains(context.NodeType))
    {
      foreach (var match in RaycastGroup)
      {
        yield return new MenuItem(match);
      }
    }
  }
}