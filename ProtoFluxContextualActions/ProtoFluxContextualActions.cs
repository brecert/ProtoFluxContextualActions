using Elements.Core;
using HarmonyLib;
using ResoniteModLoader;
using System;
using System.Linq;
using System.Reflection;
using ProtoFluxContextualActions.Attributes;

namespace ProtoFluxContextualActions;

using System.Collections.Generic;
using global::ProtoFluxContextualActions.Utils;

#if DEBUG
using ResoniteHotReloadLib;
#endif

public class ProtoFluxContextualActions : ResoniteMod
{
  private static Assembly ModAssembly => typeof(ProtoFluxContextualActions).Assembly;

  public override string Name => ModAssembly.GetCustomAttribute<AssemblyTitleAttribute>()!.Title;
  public override string Author => ModAssembly.GetCustomAttribute<AssemblyCompanyAttribute>()!.Company;
  public override string Version => ModAssembly.GetCustomAttribute<AssemblyInformationalVersionAttribute>()!.InformationalVersion;
  public override string Link => ModAssembly.GetCustomAttributes<AssemblyMetadataAttribute>().First(meta => meta.Key == "RepositoryUrl").Value!;

  internal static string HarmonyId => $"dev.bree.{ModAssembly.GetName()}";

  private static readonly Harmony harmony = new(HarmonyId);

  public static ModConfiguration? Config;

  private static readonly Dictionary<string, ModConfigurationKey<bool>> patchCategoryKeys = [];

  [AutoRegisterConfigKey]
  private static readonly ModConfigurationKey<bool> fluxStructureRelays = new("Structure Relays", "If \"Flux Structures\" should contain relays", () => true);
  [AutoRegisterConfigKey]
  private static readonly ModConfigurationKey<bool> tryFixFlick = new("Try Fix Context Flick", "If the context menu should attempt to fix flicking", () => true);
  [AutoRegisterConfigKey]
  private static readonly ModConfigurationKey<bool> tryKeepContextPosition = new("Try Keep Context Menu Position", "If the context menu should attempt to stay in the same position", () => false);
  [AutoRegisterConfigKey]
  private static readonly ModConfigurationKey<bool> defaultActionOnPrimaryRelease = new("Default Action On Primary Release", "If a display/input should be created when primary is released", () => false);
  [AutoRegisterConfigKey]
  private static readonly ModConfigurationKey<int> maxItemsPerPage = new("Max Items Per Page", "The maximum amount of items per page", () => 10);

  [AutoRegisterConfigKey]
  private static readonly ModConfigurationKey<MenuVisual> currentMenuVisual = new("Current Menu Visual", "The visual to use when rendering a menu.\t<b><color=hero.red>NOTE: No other visuals exist currently!</color></b> This setting can be ignored for now.", () => MenuVisual.ContextMenu);

  static ProtoFluxContextualActions()
  {
    DebugFunc(() => $"Static Initializing {nameof(ProtoFluxContextualActions)}...");

    var types = AccessTools.GetTypesFromAssembly(ModAssembly);

    foreach (var type in types)
    {
      var patchCategory = type.GetCustomAttribute<HarmonyPatchCategory>();
      var tweakCategory = type.GetCustomAttribute<TweakCategoryAttribute>();
      if (patchCategory != null && tweakCategory != null)
      {
        ModConfigurationKey<bool> key = new(
          name: patchCategory.info.category,
          description: tweakCategory.Description,
          computeDefault: () => tweakCategory.DefaultValue
        );

        DebugFunc(() => $"Registering patch category {key.Name}...");
        patchCategoryKeys[key.Name] = key;
      }
    }
  }

  public override void DefineConfiguration(ModConfigurationDefinitionBuilder builder)
  {
    foreach (var key in patchCategoryKeys.Values)
    {
      DebugFunc(() => $"Adding configuration key for {key.Name}...");
      builder.Key(key);
    }
  }


  public override void OnEngineInit()
  {
#if DEBUG
    HotReloader.RegisterForHotReload(this);
#endif

    Config = GetConfiguration()!;
    Config.OnThisConfigurationChanged += OnConfigChanged;

    PatchCategories();
    harmony.PatchAllUncategorized(ModAssembly);
  }


#if DEBUG
  static void BeforeHotReload()
  {
    harmony.UnpatchAll(HarmonyId);
    PsuedoGenericTypesHelper.WorldPsuedoGenericTypes.Clear();
  }

  static void OnHotReload(ResoniteMod modInstance)
  {
    PatchCategories();
    harmony.PatchAllUncategorized(ModAssembly);
  }
#endif

  private static void UnpatchCategories()
  {
    foreach (var category in patchCategoryKeys.Keys)
    {
      harmony.UnpatchCategory(ModAssembly, category);
    }
  }

  private static void PatchCategories()
  {
    foreach (var (category, key) in patchCategoryKeys)
    {
      if (Config?.GetValue(key) ?? true) // enable if fail?
      {
        harmony.PatchCategory(ModAssembly, category);
      }
    }
  }

  private static void OnConfigChanged(ConfigurationChangedEvent change)
  {
    var category = change.Key.Name;
    if (change.Key is ModConfigurationKey<bool> key && patchCategoryKeys.ContainsKey(category))
    {
      if (change.Config.GetValue(key))
      {
        DebugFunc(() => $"Patching {category}...");
        harmony.PatchCategory(category);
      }
      else
      {
        DebugFunc(() => $"Unpatching {category}...");
        harmony.UnpatchCategory(category);
      }
    }
  }

  internal static bool ShouldUseRelays() => fluxStructureRelays.Value;

  internal static bool ShouldTryFixFlick() => tryFixFlick.Value;

  internal static bool ShouldTryKeepContextPosition() => tryKeepContextPosition.Value;

  internal static int GetMaxItemsPerPage() => maxItemsPerPage.Value;

  internal static bool ShouldDoDefaultActionOnPrimaryRelease() => defaultActionOnPrimaryRelease.Value;

  internal static MenuVisual GetMenuVisual() => currentMenuVisual.Value;
}