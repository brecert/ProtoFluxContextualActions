using Elements.Core;
using HarmonyLib;
using ResoniteModLoader;
using System;
using System.Linq;
using System.Reflection;
using ProtoFluxContextualActions.Attributes;

namespace ProtoFluxContextualActions;

using System.Collections.Generic;

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

  internal static ModConfiguration? Config;

  private static readonly Dictionary<string, ModConfigurationKey<bool>> patchCategoryKeys = [];

  private static IEnumerable<string> Categories => patchCategoryKeys.Keys;

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

    Config = GetConfiguration();
    PatchCategories();
    harmony.PatchAllUncategorized(ModAssembly);
  }


#if DEBUG
  static void BeforeHotReload()
  {
    harmony.UnpatchAll(HarmonyId);
  }

  static void OnHotReload(ResoniteMod modInstance)
  {
    PatchCategories();
    harmony.PatchAllUncategorized(ModAssembly);
  }
#endif

  private static void UnpatchCategories()
  {
    foreach (var category in Categories)
    {
      harmony.UnpatchCategory(ModAssembly, category);
    }
  }

  private static void PatchCategories()
  {
    foreach (var category in Categories)
    {
      harmony.PatchCategory(ModAssembly, category);
    }
  }

  private static void OnConfigChanged(ConfigurationChangedEvent change)
  {
    var category = change.Key.Name;
    if (patchCategoryKeys.TryGetValue(category, out var key))
    {
      if (key.Value)
      {
        harmony.UnpatchCategory(category);
      }
      else
      {
        harmony.PatchCategory(category);
      }
    }
  }
}