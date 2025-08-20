using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using ProtoFluxContextualActions.Attributes;
using ProtoFluxContextualActions.Extensions;
using Elements.Core;
using FrooxEngine;
using FrooxEngine.ProtoFlux;
using HarmonyLib;
using ProtoFlux.Core;
using System.Reflection;

namespace ProtoFluxContextualActions.Patches;

[HarmonyPatchCategory("ProtoFluxTool Reference Node Creation"), TweakCategory("Adds l.")]
[HarmonyPatch(typeof(ProtoFluxTool), nameof(ProtoFluxTool.OnSecondaryPress))]
internal static class ProtoFluxTool_ContextualReferenceActions_Patch
{
  internal readonly struct MenuItem(Type node, Type? binding = null, string? name = null)
  {
    internal readonly Type node = node;

    internal readonly Type? binding = binding;

    internal readonly string? name = name;

    internal readonly string DisplayName => name ?? NodeMetadataHelper.GetMetadata(node).Name ?? node.GetNiceTypeName();
  }

  private static void AddMenuItem(ProtoFluxTool __instance, ContextMenu menu, MenuItem item, Action<ProtoFluxNode> setup)
  {
    var nodeMetadata = NodeMetadataHelper.GetMetadata(item.node);
    var label = (LocaleString)item.DisplayName;
    var menuItem = menu.AddItem(in label, (Uri?)null, item.node.GetTypeColor());
    menuItem.Button.LocalPressed += (button, data) =>
    {
      var nodeBinding = item.binding ?? ProtoFluxHelper.GetBindingForNode(item.node);
      __instance.SpawnNode(nodeBinding, n =>
          {
            n.EnsureElementsInDynamicLists();
            setup(n);
            __instance.LocalUser.CloseContextMenu(__instance);
          });
    };
  }

  static readonly Dictionary<Type, MenuItem[]> cache = [];

  internal static bool Prefix(ProtoFluxTool __instance)
  {
    var grabbedReference = __instance.GetGrabbedReference();
    if (grabbedReference == null) return true; 

    var items = cache.GetOrCreate(grabbedReference.GetType(), () => MenuItems(__instance, grabbedReference).Take(10).ToArray());

    if (items.Length != 0)
    {
      if (__instance.LocalUser.IsContextMenuOpen())
      {
        __instance.LocalUser.CloseContextMenu(__instance);
        return true;
      }

      __instance.StartTask(async () =>
      {
        var menu = await __instance.LocalUser.OpenContextMenu(__instance, __instance.Slot);

        foreach (var menuItem in items)
        {
          AddMenuItem(__instance, menu, menuItem, n =>
          {
            // todo: sanity checking rather than assuming
            if (n.NodeGlobalRefCount > 0)
            {
              var globalSyncRef = n.GetGlobalRef(0);
              var globalType = globalSyncRef.TargetType.GenericTypeArguments[0];
              var globalRef = (IGlobalValueProxy)n.Slot.AttachComponent(typeof(GlobalReference<>).MakeGenericType(globalType));
              globalSyncRef.TrySet(globalRef);
              globalRef.TrySetValue(grabbedReference);
            }
          });
        }
      });

      return false;
    }

    return true;
  }

  internal static IEnumerable<MenuItem> MenuItems(ProtoFluxTool __instance, IWorldElement? grabbedReference)
  {
    if (grabbedReference == null) yield break;

    foreach (var nodeType in NodeTypes())
    {
      var globalRefMeta = GlobalRefMetadata(nodeType).Where(m => !m.ValueType.IsGenericTypeDefinition).FirstOrDefault();
      if (globalRefMeta != null && globalRefMeta.ValueType.IsAssignableFrom(grabbedReference.GetType()))
      {
        yield return new MenuItem(nodeType);
      }
    }
  }

  // lighter than GetMetadata
  internal static IEnumerable<GlobalRefMetadata> GlobalRefMetadata(Type type)
  {
    var index = 0;
    foreach (var field in type.EnumerateAllInstanceFields(BindingFlags.Instance | BindingFlags.Public))
    {
      if (field.FieldType.IsGenericType && field.FieldType.GetGenericTypeDefinition() == typeof(GlobalRef<>))
      {
        yield return new GlobalRefMetadata(index++, field);
      }
    }
  }

  public static IEnumerable<Type> NodeTypes() =>
    Traverse.Create(typeof(ProtoFluxHelper)).Field<Dictionary<Type, Type>>("protoFluxToBindingMapping").Value.Keys;

  [HarmonyReversePatch]
  [HarmonyPatch(typeof(ProtoFluxHelper), "GetNodeForType")]
  [MethodImpl(MethodImplOptions.NoInlining)]
  internal static Type GetNodeForType(Type type, List<NodeTypeRecord> list) => throw new NotImplementedException();
}