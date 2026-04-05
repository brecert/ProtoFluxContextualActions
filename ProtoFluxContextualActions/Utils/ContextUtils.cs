using System;
using System.Collections.Generic;
using System.Linq;
using FrooxEngine.ProtoFlux;
using HarmonyLib;
using Elements.Core;
using static ProtoFluxContextualActions.Patches.ContextualSelectionActionsPatch;
using FrooxEngine;
using System.Threading.Tasks;

namespace ProtoFluxContextualActions.Utils;

internal static class ContextUtils
{
  internal static async Task<ContextMenu> CreateContextMenu(ProtoFluxTool tool)
  {
    var menu = await tool.LocalUser.OpenContextMenu(tool, tool.Slot);
    // for some reason, the pre-remake version used 10 and its consistent, but here 10 doesnt work.
    // todo: look into why this is?
    var menuFields = Traverse.Create(menu);
    menuFields.Field<float?>("_speedOverride").Value = 12; // faster for better swiping
    // stupid attempt to force the contextmenu to use flick, as it tends to fail in vr at lower framerates?
    _ = tool.StartTask(async () =>
    {
      await new Updates(3);
      var user = Engine.Current.WorldManager.FocusedWorld.LocalUser;
      var controller = user.InputInterface.GetControllerNode(tool.ActiveHandler.Side);
      menuFields.Field<Sync<bool>>("_flickModeActive").Value.Value = controller.ActionPrimary.Held;
    });
    return menu;
  }

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