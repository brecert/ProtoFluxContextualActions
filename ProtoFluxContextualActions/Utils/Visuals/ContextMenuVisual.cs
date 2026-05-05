using System;
using System.Threading.Tasks;
using FrooxEngine;
using FrooxEngine.ProtoFlux;

namespace ProtoFluxContextualActions.Utils.Visuals;

internal class ContextMenuVisual : IMenuVisual
{
  ContextMenu? currentMenu;
  ProtoFluxTool? currentTool;
  bool isInitialMenu = true;
  async Task IMenuVisual.CreateInitialMenu(ProtoFluxTool tool)
  {
    currentTool = tool;
  }

  async Task IMenuVisual.OnNewFolder()
  {
    currentMenu = await ContextMenuCreator.CreateMenu(currentTool!, isInitialMenu);
    isInitialMenu = false;
  }

  async Task IMenuVisual.OnRenderDone()
  {

  }

  async Task IMenuVisual.RenderBack(Action onClicked)
  {
    currentMenu!.AddMenuItem("Back", RadiantUI_Constants.Hero.RED, onClicked);
  }

  async Task IMenuVisual.RenderItem(GroupItem item, Action onClicked)
  {
    currentMenu!.AddMenuItem(item.name, item.color, onClicked, item.iconUri);
  }

  async Task IMenuVisual.RenderNextPage(Action onClicked)
  {
    currentMenu!.AddMenuItem("Next", RadiantUI_Constants.Hero.CYAN, onClicked);
  }

  async Task IMenuVisual.RenderPrevPage(Action onClicked)
  {
    currentMenu!.AddMenuItem("Previous", RadiantUI_Constants.Hero.ORANGE, onClicked);
  }
}