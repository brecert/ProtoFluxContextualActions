using ProtoFlux.Runtimes.Execution.Nodes.FrooxEngine.Nodes;

namespace ProtoFluxContextualActions.Patches;

static partial class ContextualSwapActionsPatch
{
  static readonly HashSet<Type> ProtoFluxPackingGroup = [
    typeof(UnpackProtoFlux),
    typeof(PackProtoFluxFromNode),
    typeof(PackProtoFluxInPlace),
    typeof(PackProtoFluxNodes),
  ];
  internal static IEnumerable<MenuItem> ProtoFluxPackingGroupItems(ContextualContext context)
  {
    if (ProtoFluxPackingGroup.Contains(context.NodeType))
    {
      foreach (var match in ProtoFluxPackingGroup)
      {
        yield return new MenuItem(match);
      }
    }
  }
}