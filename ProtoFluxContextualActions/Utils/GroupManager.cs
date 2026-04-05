using System;
using System.Collections.Generic;
using System.Linq;
using FrooxEngine.ProtoFlux;
using HarmonyLib;
using Elements.Core;
using static ProtoFluxContextualActions.Patches.ContextualSelectionActionsPatch;

namespace ProtoFluxContextualActions.NewScripts;

internal struct ContextItem
{
  internal string name;
  internal colorX? color;
  internal Action onClick;
  internal Uri? iconUri;
  internal MenuItem baseItem;
}

internal class GroupManager
{
  // Page Config
  const int MaxPerPage = 10; // Maximum possible items in the context menu, excluding Previous/Next/Back buttons
  const bool ShowBackOnAllPages = false;

  // Folder Config
  // - 'White 128' version of the folder icon from resonite
  static readonly Uri FolderIcon = new("resdb:///c8628c05dc2c5a047d90455da53ada83d3d4a2279662efbe156e2147f893f5b0.png");

  // Instance Variables
  readonly Dictionary<string, List<List<ContextItem>>> PagedGroups = [];
  readonly List<ContextItem> RootItems = [];
  readonly Action<MenuItem> onItemClicked;
  readonly ProtoFluxTool currentTool;

  // Possibly allow for sorting groups using "1-[Name]" where "1" is the order, and gets displayed as [Name]?
  // Maybe also allow for MenuItem to be sorted with OrderOffset (like slots)?
  internal GroupManager(ProtoFluxTool tool, List<MenuItem> items, colorX? targetColor, Action<MenuItem> onClicked)
  {
    onItemClicked = onClicked;

    List<ContextItem> contextItems = items.Select((item) =>
    {
      colorX itemColor = targetColor ?? item.node.GetTypeColor();
      return new ContextItem()
      {
        name = item.DisplayName,
        color = targetColor,
        onClick = () => onItemClicked(item),
        baseItem = item,
      };
    }).ToList();

    Dictionary<string, List<ContextItem>> GroupedItems = [];
    contextItems.ForEach((item) =>
    {
      string itemGroup = item.baseItem.group;
      if (itemGroup == "") RootItems.Add(item);
      else if (GroupedItems.TryGetValue(itemGroup, out List<ContextItem>? list)) list.Add(item);
      else GroupedItems.Add(itemGroup, [item]);
    });

    PagedGroups = GroupedItems.ToDictionary(kv => kv.Key, kv => kv.Value.SplitToGroups(MaxPerPage));
    currentTool = tool;
  }

  internal bool RenderRoot()
  {
    if (currentTool.IsRemoved) return false;
    if (PagedGroups.Count + RootItems.Count == 0) return false;
    if (PagedGroups.Count != 1 || RootItems.Count != 0)
    {
      List<ContextItem> currentRootItems = [];
      foreach (var group in PagedGroups)
      {
        currentRootItems.Add(new()
        {
          name = group.Key,
          color = colorX.White,
          onClick = () => RenderFolder(group.Value, 0, false),
          iconUri = FolderIcon
        });
      }
      currentRootItems.AddRange(RootItems);

      List<List<ContextItem>> pagedRootItems = currentRootItems.SplitToGroups(MaxPerPage);
      RenderFolder(pagedRootItems, 0, true);
    }
    else if (PagedGroups.Count == 0) return false;
    else RenderFolder(PagedGroups.Values.ToList()[0], 0, true);
    return true;
  }

  void RenderFolder(List<List<ContextItem>> Items, int pageIndex, bool isRoot = false)
  {
    if (currentTool.IsRemoved) return;
    bool showPreviousButton = pageIndex > 0;
    bool showNextButton = pageIndex < Items.Count - 1;
    bool showBackButton =
            (Items.Count + RootItems.Count) != 1
            && (ShowBackOnAllPages || pageIndex == 0)
            && !isRoot;


    currentTool.StartTask(async () =>
    {
      var menu = await ContextUtils.CreateContextMenu(currentTool);

      if (showBackButton)
      {
        menu.AddMenuItem("Back", colorX.Red, () => RenderRoot());
      }
      if (showPreviousButton)
      {
        menu.AddMenuItem("Previous", colorX.Orange, () => RenderFolder(Items, pageIndex - 1, isRoot));
      }

      foreach (var item in Items[pageIndex])
      {
        menu.AddMenuItem(item.name, item.color, item.onClick, item.iconUri);
      }


      if (showNextButton)
      {
        menu.AddMenuItem("Next", colorX.Cyan, () => RenderFolder(Items, pageIndex + 1, isRoot));
      }
    });
  }
}