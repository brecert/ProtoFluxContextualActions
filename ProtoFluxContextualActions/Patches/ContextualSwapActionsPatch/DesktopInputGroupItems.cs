using System;
using System.Collections.Generic;
using ProtoFlux.Runtimes.Execution.Nodes.FrooxEngine.Input.Display;
using ProtoFlux.Runtimes.Execution.Nodes.FrooxEngine.Input.Mouse;
using ProtoFlux.Runtimes.Execution.Nodes.FrooxEngine.Slots;

namespace ProtoFluxContextualActions.Patches;

static partial class ContextualSwapActionsPatch
{
  static readonly HashSet<Type> DesktopMouseHeldGroup = [
    typeof(LeftMouseHeld),
    typeof(RightMouseHeld),
    typeof(MiddleMouseHeld),
  ];
  static readonly HashSet<Type> DesktopMousePressedGroup = [
    typeof(LeftMousePressed),
    typeof(RightMousePressed),
    typeof(MiddleMousePressed),
  ];
  static readonly HashSet<Type> DesktopMouseReleasedGroup = [
    typeof(LeftMouseReleased),
    typeof(RightMouseReleased),
    typeof(MiddleMouseReleased),
  ];

  static readonly HashSet<Type> DesktopLeftMouseGroup = [
    typeof(LeftMouseHeld),
    typeof(LeftMousePressed),
    typeof(LeftMouseReleased),
  ];
  static readonly HashSet<Type> DesktopRightMouseGroup = [
    typeof(RightMouseHeld),
    typeof(RightMousePressed),
    typeof(RightMouseReleased),
  ];
  static readonly HashSet<Type> DesktopMiddleMouseGroup = [
    typeof(MiddleMouseHeld),
    typeof(MiddleMousePressed),
    typeof(MiddleMouseReleased),
  ];

  static readonly HashSet<Type> DesktopMousePositionGroup = [
    typeof(MouseScrollDelta2D),
    typeof(DesktopMousePosition),
    typeof(MouseMovementDelta),
    typeof(MousePosition),
    typeof(NormalizedMousePosition),
  ];

  static readonly HashSet<Type> DesktopWindowGroup = [
    typeof(LocalWindowResolution),
    typeof(LocalPrimaryResolution),
  ];

  internal static IEnumerable<MenuItem> DesktopInputGroupItems(ContextualContext context)
  {
    if (DesktopMouseHeldGroup.Contains(context.NodeType))
    {
      foreach (var match in DesktopMouseHeldGroup)
      {
        yield return new MenuItem(match);
      }
    }
    if (DesktopMousePressedGroup.Contains(context.NodeType))
    {
      foreach (var match in DesktopMousePressedGroup)
      {
        yield return new MenuItem(match);
      }
    }
    if (DesktopMouseReleasedGroup.Contains(context.NodeType))
    {
      foreach (var match in DesktopMouseReleasedGroup)
      {
        yield return new MenuItem(match);
      }
    }

    if (DesktopLeftMouseGroup.Contains(context.NodeType))
    {
      foreach (var match in DesktopLeftMouseGroup)
      {
        yield return new MenuItem(match);
      }
    }
    if (DesktopRightMouseGroup.Contains(context.NodeType))
    {
      foreach (var match in DesktopRightMouseGroup)
      {
        yield return new MenuItem(match);
      }
    }
    if (DesktopMiddleMouseGroup.Contains(context.NodeType))
    {
      foreach (var match in DesktopMiddleMouseGroup)
      {
        yield return new MenuItem(match);
      }
    }

    if (DesktopMousePositionGroup.Contains(context.NodeType))
    {
      foreach (var match in DesktopMousePositionGroup)
      {
        yield return new MenuItem(match);
      }
    }
    if (DesktopWindowGroup.Contains(context.NodeType))
    {
      foreach (var match in DesktopWindowGroup)
      {
        yield return new MenuItem(match);
      }
    }
  }
}