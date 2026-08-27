using System;
using System.Threading.Tasks;
using FrooxEngine.ProtoFlux;

namespace ProtoFluxContextualActions.Utils.Visuals;

internal interface IMenuVisual
{
  internal Task CreateInitialMenu(ProtoFluxTool tool);
  internal Task OnNewFolder();
  internal Task RenderItem(GroupItem item, Action onClicked);
  internal Task RenderBack(Action onClicked);
  internal Task RenderNextPage(Action onClicked);
  internal Task RenderPrevPage(Action onClicked);
  internal Task OnRenderDone();
}