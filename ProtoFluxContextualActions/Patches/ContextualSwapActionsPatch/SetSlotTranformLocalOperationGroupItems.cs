using ProtoFluxContextualActions.Tagging;

namespace ProtoFluxContextualActions.Patches;

static partial class ContextualSwapActionsPatch
{
  internal static IEnumerable<MenuItem> SetSlotTranformLocalOperationGroupItems(ContextualContext context)
  {
    if (Groups.SetSlotTranformLocalOperationGroup.Contains(context.NodeType))
    {
      foreach (var match in Groups.SetSlotTranformLocalOperationGroup)
      {
        yield return new MenuItem(match);
      }
    }
  }
}