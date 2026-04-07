using System;
using System.Collections.Generic;
using System.Linq;
using FrooxEngine.ProtoFlux;
using HarmonyLib;
using Elements.Core;
using FrooxEngine;
using System.Threading.Tasks;
using System.ComponentModel;

namespace ProtoFluxContextualActions.Utils;

internal class ContextMenuCreator
{
  internal async Task<ContextMenu> CreateContextMenu(ProtoFluxTool tool, bool isInitialMenu = false)
  {
    bool shouldKeepPositionConfig = ProtoFluxContextualActions.ShouldTryKeepContextPosition();
    bool shouldKeepPosition = !isInitialMenu && shouldKeepPositionConfig;
    var menu = await tool.LocalUser.OpenContextMenu(tool, tool.Slot, options: new ContextMenuOptions { speedOverride = 12, keepPosition = shouldKeepPosition });

    // stupid attempt to force the contextmenu to use flick, as it tends to fail in vr at lower framerates?
    _ = tool.StartTask(async () =>
    {
      bool shouldTryFixFlick = ProtoFluxContextualActions.ShouldTryFixFlick();
      if (!shouldTryFixFlick) return;
      await new Updates(3);
      var user = Engine.Current.WorldManager.FocusedWorld.LocalUser;
      var controller = user.InputInterface.GetControllerNode(tool.ActiveHandler.Side);
      Traverse.Create(menu).Field<Sync<bool>>("_flickModeActive").Value.Value = controller.ActionPrimary.Held;
    });
    return menu;
  }

  internal static Task<ContextMenu> CreateMenu(ProtoFluxTool tool, bool isInitialMenu = false)
  {
    return new ContextMenuCreator().CreateContextMenu(tool, isInitialMenu);
  }
}

internal static class ContextExtensions
{
  internal static void AddMenuItem(this ContextMenu menu, string name, colorX? color, Action onClicked, Uri? icon = null)
  {
    var label = (LocaleString)name;
    var menuItem = menu.AddItem(in label, icon, color);
    menuItem.Button.LocalPressed += (button, data) =>
    {
      onClicked();
    };
  }
}