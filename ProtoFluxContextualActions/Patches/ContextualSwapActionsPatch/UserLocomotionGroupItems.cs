using ProtoFlux.Runtimes.Execution.Nodes.FrooxEngine.Locomotion;

namespace ProtoFluxContextualActions.Patches;

static partial class ContextualSwapActionsPatch
{
  static readonly HashSet<Type> UserLocomotionGroup = [
    typeof(SwitchLocomotionModule),
    typeof(InstallLocomotionModules),
  ];

  internal static IEnumerable<MenuItem> UserLocomotionGroupItems(ContextualContext context)
  {
    if (UserLocomotionGroup.Contains(context.NodeType))
    {
      foreach (var match in UserLocomotionGroup)
      {
        yield return new MenuItem(match);
      }
    }
  }
}