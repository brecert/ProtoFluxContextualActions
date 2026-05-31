using System;
using System.Collections.Generic;
using System.Linq;
using FrooxEngine.ProtoFlux;
using HarmonyLib;
using Elements.Core;
using static ProtoFluxContextualActions.Patches.ContextualSelectionActionsPatch;
using FrooxEngine;
using ProtoFluxContextualActions.Utils.Visuals;

namespace ProtoFluxContextualActions.Utils;

internal struct GroupItem
{
  internal string name;
  internal colorX? color;
  internal Action onClick;
  internal Uri? iconUri;
  internal MenuItem baseItem;
}

// To be used by the mod config, as a "What visual do you want to use" setting.
// Add new entries when making a new visual
internal enum MenuVisual
{
  ContextMenu
}

internal class GroupManager
{
  // Page Config
  int MaxPerPage => ProtoFluxContextualActions.GetMaxItemsPerPage(); // Maximum possible items in the context menu, excluding Previous/Next/Back buttons
  const bool ShowBackOnAllPages = false;

  // Folder Config
  // - 'White 128' version of the folder icon from resonite
  static readonly Uri FolderIcon = new("resdb:///c8628c05dc2c5a047d90455da53ada83d3d4a2279662efbe156e2147f893f5b0.png");
  static readonly Uri CollapsedGroupIcon = new("resdb:///cf7390db8e2686e8fdf18cc4526ef82113b002e07254eb925c5e69ba6ef27e59.png");

  // Instance Variables
  readonly Dictionary<string, List<GroupItem>> GroupedItems = [];
  readonly Action<MenuItem> onItemClicked;
  readonly ProtoFluxTool currentTool;

  readonly IMenuVisual currentVisual;

  internal GroupManager(ProtoFluxTool tool, List<MenuItem> items, colorX? targetColor, Action<MenuItem> onClicked)
  {
    onItemClicked = onClicked;

    List<GroupItem> contextItems = [.. items.Select((item) =>
    {
      colorX itemColor = targetColor ?? item.node.GetTypeColor();
      return new GroupItem()
      {
        name = item.DisplayName,
        color = targetColor,
        onClick = () => onItemClicked(item),
        baseItem = item,
      };
    })];

    contextItems.ForEach((item) =>
    {
      string itemGroup = item.baseItem.group;
      if (GroupedItems.TryGetValue(itemGroup, out List<GroupItem>? list)) list.Add(item);
      else GroupedItems.Add(itemGroup, [item]);
    });

    // Would be read from mod config instead of a constant
    MenuVisual selectedVisual = MenuVisual.ContextMenu;

    currentVisual = selectedVisual switch
    {
      MenuVisual.ContextMenu => new ContextMenuVisual(),
      // add custom visual classes here, with a new enum entry
      _ => new ContextMenuVisual(),
    };
    currentVisual.CreateInitialMenu(tool);

    currentTool = tool;
  }

  List<GroupItem> GetLevelItems(string prefix, out List<string> subgroups)
  {
    subgroups = [];
    var items = new List<GroupItem>();
    var subgroupSet = new HashSet<string>();

    foreach (var kv in GroupedItems)
    {
      string key = kv.Key;

      if (string.IsNullOrEmpty(prefix) && string.IsNullOrEmpty(key)) items.AddRange(kv.Value);

      if (prefix.Length == 0)
      {
        if (!key.Contains("/"))
        {
          subgroupSet.Add(key);
        }
        else
        {
          var first = key.Split('/')[0];
          subgroupSet.Add(first);
        }

        continue;
      }

      if (!key.StartsWith(prefix + "/") && key != prefix)
        continue;

      if (key == prefix)
      {
        items.AddRange(kv.Value);
        continue;
      }

      var remaining = key[(prefix.Length + 1)..];

      int slash = remaining.IndexOf('/');
      if (slash >= 0)
      {
        var next = remaining[..slash];
        subgroupSet.Add(next);
      }
      else
      {
        subgroupSet.Add(remaining);
      }

    }

    subgroups = [.. subgroupSet];
    return items;
  }

  string GetParentPrefix(string prefix)
  {
    if (string.IsNullOrEmpty(prefix)) return "";

    int index = prefix.LastIndexOf('/');
    if (index < 0) return "";

    return prefix[..index];
  }

  List<List<GroupItem>> BuildPages(string prefix)
  {
    var items = GetLevelItems(prefix, out var subgroups);

    List<GroupItem> combined = [];

    foreach (var subgroup in subgroups)
    {
      string path = string.IsNullOrEmpty(prefix)
        ? subgroup
        : $"{prefix}/{subgroup}";

      combined.Add(BuildFolderEntry(path, subgroup));
    }

    combined.AddRange(items);

    return SplitGroups2(combined);
  }

  GroupItem BuildFolderEntry(string path, string displayName)
  {
    string currentPath = path;
    string currentName = displayName;

    // This should never run out
    for (int i = 0; i < 8; i++)
    {
      var items = GetLevelItems(currentPath, out var subgroups);

      int childCount = items.Count + subgroups.Count;

      if (childCount != 1)
      {
        return new()
        {
          name = currentName,
          color = RadiantUI_Constants.Neutrals.LIGHT,
          onClick = () => RenderGroup(currentPath, 0, false),
          iconUri = FolderIcon
        };
      }

      if (items.Count == 1)
      {
        GroupItem item = items[0];

        item.name = $"{currentName} > {item.name}";
        item.iconUri = CollapsedGroupIcon;
        item.color = RadiantUI_Constants.Neutrals.LIGHT;
        return item;
      }

      string nextGroup = subgroups[0];

      currentName += " > " + nextGroup;
      currentPath += "/" + nextGroup;
    }
    // If it does run out however..
    return new() { name = "Something Broke!", color = colorX.Red };
  }

  internal List<List<T>> SplitGroups2<T>(List<T> items)
  {
    var groups = items.SplitToGroups(MaxPerPage);
    if (groups.Count == 2 && groups[1].Count == 1)
    {
      return [[.. groups[0], .. groups[1]]];
    }
    return groups;
  }

  internal bool RenderRoot(bool initialMenu = false)
  {
    if (currentTool.IsRemoved) return false;
    if (GroupedItems.Count == 0) return false;

    var rootPages = BuildPages("");
    List<GroupItem> rootItems = [];
    if (GroupedItems.TryGetValue("", out var curItems)) rootItems = curItems;

    if (rootPages.Count != 1 || rootItems.Count != 0)
    {
      List<GroupItem> currentRootItems = [];

      var items = GetLevelItems("", out var subgroups);

      foreach (var subgroup in subgroups)
      {
        string path = subgroup;

        if (string.IsNullOrEmpty(path))
          continue;

        currentRootItems.Add(BuildFolderEntry(path, subgroup));
      }

      currentRootItems.AddRange(items);

      List<List<GroupItem>> pagedRootItems = SplitGroups2(currentRootItems);
      RenderFolder(pagedRootItems, 0, true, initialMenu, "");
    }
    else if (GroupedItems.Count == 0) return false;
    else RenderFolder(rootPages, 0, true, initialMenu, "");

    return true;
  }

  void RenderGroup(string prefix, int pageIndex, bool isRoot = false, bool initialMenu = false)
  {
    var pages = BuildPages(prefix);
    RenderFolder(pages, pageIndex, isRoot, initialMenu, prefix);
  }

  void RenderFolder(List<List<GroupItem>> Items, int pageIndex, bool isRoot = false, bool initialMenu = false, string prefix = "")
  {
    if (currentTool.IsRemoved) return;
    bool showPreviousButton = pageIndex > 0;
    bool showNextButton = pageIndex < Items.Count - 1;
    bool showBackButton =
            GroupedItems.Count != 1
            && (ShowBackOnAllPages || pageIndex == 0)
            && !isRoot;


    currentTool.StartTask(async () =>
    {
      await currentVisual.OnNewFolder();

      if (showBackButton)
      {
        await currentVisual.RenderBack(() =>
        {
          if (string.IsNullOrEmpty(prefix))
          {
            RenderRoot();
          }
          else
          {
            string parentFolder = GetParentPrefix(prefix);
            if (string.IsNullOrEmpty(parentFolder))
            {
              RenderRoot();
            }
            else
            {
              RenderGroup(parentFolder, 0);
            }
          }
        });
      }
      if (showPreviousButton)
      {
        await currentVisual.RenderPrevPage(() => RenderFolder(Items, pageIndex - 1, isRoot, false, prefix));
      }

      foreach (var item in Items[pageIndex])
      {
        await currentVisual.RenderItem(item, item.onClick);
      }


      if (showNextButton)
      {
        await currentVisual.RenderNextPage(() => RenderFolder(Items, pageIndex + 1, isRoot, false, prefix));
      }
    });
  }
}