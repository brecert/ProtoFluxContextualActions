using System;
using System.Collections.Generic;
using System.Linq;
using FrooxEngine.ProtoFlux;
using HarmonyLib;
using Elements.Core;
using static ProtoFluxContextualActions.Patches.ContextualSelectionActionsPatch;
using FrooxEngine;

namespace ProtoFluxContextualActions.Utils;

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
  int MaxPerPage => ProtoFluxContextualActions.GetMaxItemsPerPage(); // Maximum possible items in the context menu, excluding Previous/Next/Back buttons
  const bool ShowBackOnAllPages = false;

  // Folder Config
  // - 'White 128' version of the folder icon from resonite
  static readonly Uri FolderIcon = new("resdb:///c8628c05dc2c5a047d90455da53ada83d3d4a2279662efbe156e2147f893f5b0.png");

  // Instance Variables
  readonly Dictionary<string, List<ContextItem>> GroupedItems = [];
  readonly Action<MenuItem> onItemClicked;
  readonly ProtoFluxTool currentTool;

  internal GroupManager(ProtoFluxTool tool, List<MenuItem> items, colorX? targetColor, Action<MenuItem> onClicked)
  {
    onItemClicked = onClicked;

    List<ContextItem> contextItems = [.. items.Select((item) =>
    {
      colorX itemColor = targetColor ?? item.node.GetTypeColor();
      return new ContextItem()
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
      if (GroupedItems.TryGetValue(itemGroup, out List<ContextItem>? list)) list.Add(item);
      else GroupedItems.Add(itemGroup, [item]);
    });

    currentTool = tool;
  }

  List<ContextItem> GetLevelItems(string prefix, out List<string> subgroups)
  {
    subgroups = [];
    var items = new List<ContextItem>();
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

  List<List<ContextItem>> BuildPages(string prefix)
  {
    var items = GetLevelItems(prefix, out var subgroups);

    List<ContextItem> combined = [];

    foreach (var subgroup in subgroups)
    {
      string path = string.IsNullOrEmpty(prefix) ? subgroup : $"{prefix}/{subgroup}";
      combined.Add(new()
      {
        name = subgroup,
        color = RadiantUI_Constants.Neutrals.LIGHT,
        onClick = () => RenderGroup(path, 0, false),
        iconUri = FolderIcon
      });
    }

    combined.AddRange(items);

    return SplitGroups2(combined);
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
    List<ContextItem> rootItems = [];
    if (GroupedItems.TryGetValue("", out var curItems)) rootItems = curItems;

    if (rootPages.Count != 1 || rootItems.Count != 0)
    {
      List<ContextItem> currentRootItems = [];

      var items = GetLevelItems("", out var subgroups);

      foreach (var subgroup in subgroups)
      {
        string path = subgroup;
        if (string.IsNullOrEmpty(path)) continue;
        currentRootItems.Add(new()
        {
          name = subgroup,
          color = RadiantUI_Constants.Neutrals.LIGHT,
          onClick = () => RenderGroup(path, 0, false, initialMenu),
          iconUri = FolderIcon
        });
      }

      currentRootItems.AddRange(items);

      List<List<ContextItem>> pagedRootItems = SplitGroups2(currentRootItems);
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

  void RenderFolder(List<List<ContextItem>> Items, int pageIndex, bool isRoot = false, bool initialMenu = false, string prefix = "")
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
      var menu = await ContextMenuCreator.CreateMenu(currentTool, initialMenu);

      if (showBackButton)
      {
        menu.AddMenuItem("Back", RadiantUI_Constants.Hero.RED, () =>
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
        menu.AddMenuItem("Previous", RadiantUI_Constants.Hero.ORANGE, () => RenderFolder(Items, pageIndex - 1, isRoot, false, prefix));
      }

      foreach (var item in Items[pageIndex])
      {
        menu.AddMenuItem(item.name, item.color, item.onClick, item.iconUri);
      }


      if (showNextButton)
      {
        menu.AddMenuItem("Next", RadiantUI_Constants.Hero.CYAN, () => RenderFolder(Items, pageIndex + 1, isRoot, false, prefix));
      }
    });
  }
}