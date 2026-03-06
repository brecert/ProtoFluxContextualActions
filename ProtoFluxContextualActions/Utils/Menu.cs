using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Elements.Core;
using FrooxEngine;
using FrooxEngine.UIX;
using HarmonyLib;
using ProtoFluxContextualActions.Extensions;

namespace ProtoFluxContextualActions.Utils;

public static class MenuHelper
{
  public interface INodeMenuItem
  {
    string NodeName { get; }
    string ElementName { get; }
    string NodePath { get; }

    ButtonEventHandler Handler { get; }
  }

  public struct NodeMenuItem(string nodeName, string elementName, string nodePath, float nodeIndexFraction, ButtonEventHandler handler) : INodeMenuItem
  {
    public readonly string NodeName => nodeName;

    public readonly string ElementName => elementName;

    public readonly string NodePath => nodePath;

    public readonly float NodeIndexFraction => nodeIndexFraction;

    public readonly ButtonEventHandler Handler => handler;
  }

  public static UIBuilder SetupPanel(Slot root, LocaleString label, float2 size)
  {
    var uiContainer = root.AttachComponent<GenericUIContainer>();
    uiContainer.CloseDestroyRoot.Target = root;
    uiContainer.Title.Target = root.Name_Field;
    uiContainer.ContainerTitle.Value = label.ToString();
    var canvas = root.AttachComponent<Canvas>();
    canvas.Size.Value = size;
    root.AttachComponent<Grabbable>().Scalable.Value = true;
    var ui = new UIBuilder(canvas);
    RadiantUI_Constants.SetupDefaultStyle(ui);
    ui.PushStyle();
    var spriteProvider = root.AttachSprite(OfficialAssets.Graphics.UI.Circle.Light_Border.Circle_Phi2);
    spriteProvider.Borders.Value = float4.One * 0.5f;
    spriteProvider.FixedSize.Value = 24;
    ui.Panel(RadiantUI_Constants.BG_COLOR, spriteProvider, ui.Style.NineSliceSizing, zwrite: true);
    ui.Next("Content");
    ui.Nest();
    ui.CurrentRect.AddFixedPadding(8f);
    ui.PopStyle();
    return ui;
  }

  public static void BuildMenu(Slot slot, IEnumerable<NodeMenuItem> items, out TextField searchField)
  {
    slot.PositionInFrontOfUser(float3.Backward, distance: 1f);
    slot.LocalScale *= 0.00075f;
    slot.LocalPosition += new float3(0, -0.15f, -0.01f);
    var ui = SetupPanel(slot, "Search Panel", size: new(384, 512));
    ui.VerticalLayout(
      spacing: 4,
      forceExpandHeight: false
    );

    ui.PushStyle();
    ui.Style.PreferredHeight = 48;
    ui.Next("Search Field");
    ui.Nest();
    searchField = ui.TextField(undo: true, parseRTF: false, promptText: "Search...");
    ui.PopStyle();
    ui.NestOut();

    ui.PushStyle();
    ui.Style.FlexibleHeight = 1;
    ui.Next("Elements");
    ui.Nest();
    ui.VerticalLayout(spacing: 4);
    ui.ScrollArea();
    ui.VerticalLayout(spacing: 2, 0, childAlignment: Alignment.TopCenter);
    ui.FitContent(horizontal: SizeFit.Disabled, vertical: SizeFit.PreferredSize);
    ui.PopStyle();

    ui.PushStyle();
    ui.Style.FlexibleHeight = 0;
    ui.Style.PreferredHeight = 48;
    List<(NodeMenuItem item, Slot slot)> itemSlots = [];
    foreach (var (i, item) in items.Index())
    {
      var button = ui.Button("", RadiantUI_Constants.BUTTON_COLOR);
      // if (i > 10) button.Slot.ActiveSelf = false;
      itemSlots.Add((item, button.Slot));
      button.LocalPressed += item.Handler;
      ui.NestInto(button.Slot);
      ui.VerticalLayout(
        spacing: 4,
        4, 8, 4, 8,
        forceExpandWidth: true,
        forceExpandHeight: true
      );
      {
        var h = ui.HorizontalLayout(spacing: 0);
        {
          h.HorizontalAlign.Value = LayoutHorizontalAlignment.Justify;
          ui.Text(item.NodeName, 24, bestFit: true, parseRTF: true, alignment: Alignment.MiddleLeft);
          ui.Text(item.ElementName, 24, bestFit: true, parseRTF: true, alignment: Alignment.MiddleRight);
          ui.NestOut();
        }
        ui.Text(item.NodePath, 16, bestFit: true, parseRTF: false, alignment: Alignment.MiddleLeft);
      }
      ui.NestOut();
      ui.NestOutFrom(button.Slot);
    }
    ui.PopStyle();

    UpdateSlots(itemSlots, "");
    searchField.TargetStringField.Changed += (a) =>
    {
      var text = ((IField<string>)a).Value.ToLower();
      UpdateSlots(itemSlots, text);

      // itemSlots.Sort((a, b) => Score(a, text).CompareTo(Score(b, text)));

      // foreach (var ((item, slot), i) in itemSlots.WithIndex())
      // {
      //   slot.OrderOffset = (long)(Score(item.NodeName, text) * -10000f);
      // }

      // static float Score(string a, string b) => a.IndexOf(b);

    };
  }

  private static void UpdateSlots(IEnumerable<(NodeMenuItem, Slot)> itemSlots, string text)
  {
    if (text == "")
    {
      foreach (var (i, (item, slot)) in itemSlots.OrderBy(a => a.Item1.NodeName).Index())
      {
        var nodeName = item.NodeName.ToLower();
        var elementName = item.ElementName.ToLower();
        var nodePath = item.NodePath.ToLower();
        slot.OrderOffset = i;
      }
      return;
    }

    // var i = 0;
    foreach (var (item, slot) in itemSlots)
    {
      // if (i > 10)
      // {
      //   slot.ActiveSelf = false;
      //   continue;
      // }


      var nodeName = item.NodeName.ToLower();
      var elementName = item.ElementName.ToLower();
      var nodePath = item.NodePath.ToLower();
      var query = text.ToLower().Split(' ', StringSplitOptions.RemoveEmptyEntries);

      // var x = nodeName.IndexOf(text) + 1;
      // todo: this does not give great results.
      // var score = nodeName.Length;
      var score = query.Aggregate(0f, (a, x) => a + (nodeName.IndexOf(x) is var i and >= 0 ? nodeName.Length / (nodeName.Length + i + 1f) : 0))
                + query.Aggregate(0f, (a, x) => a + (elementName.IndexOf(x) is var i and >= 0 ? elementName.Length / (elementName.Length + i + 1f) : 0))
                + (nodePath.Contains(text) ? 0.00001f : 0f);

      UniLog.Log(query.Join(delimiter: ","));
      UniLog.Log(score);

      slot.ActiveSelf = text == "" || score > 0;
      slot.OrderOffset = text == "" ? 0 : (long)(score * -10000000f);

      // if (slot.ActiveSelf) i++;
    }
  }
}