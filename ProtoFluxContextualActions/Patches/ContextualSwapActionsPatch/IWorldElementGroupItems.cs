using ProtoFlux.Runtimes.Execution.Nodes.FrooxEngine.Elements;
using ProtoFlux.Runtimes.Execution.Nodes.FrooxEngine.References;

namespace ProtoFluxContextualActions.Patches;

static partial class ContextualSwapActionsPatch
{
  static readonly HashSet<Type> IWorldElementGroup = [
    typeof(IsDestroyed),
    typeof(IsRemoved),
    typeof(AllocatingUser),
  ];
  internal static IEnumerable<MenuItem> IWorldElementGroupItems(ContextualContext context)
  {
    if (IWorldElementGroup.Contains(context.NodeType))
    {
      foreach (var match in IWorldElementGroup)
      {
        yield return new MenuItem(match);
      }
    }
  }
}