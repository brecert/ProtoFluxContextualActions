using Elements.Core;
using HarmonyLib;
using ResoniteModLoader;
using System;
using System.Linq;
using System.Reflection;
using ProtoFluxContextualActions.Attributes;
using ProtoFluxContextualActions.Extensions;

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

  private static ModConfiguration? config;

  private static readonly Dictionary<string, ModConfigurationKey<bool>> patchCategoryKeys = [];

  static ProtoFluxContextualActions()
  {
    DebugFunc(() => $"Static Initializing {nameof(ProtoFluxContextualActions)}...");

    var types = AccessTools.GetTypesFromAssembly(ModAssembly);

    var categoryKeys = types
      .Select(t => (patchCategory: t.GetCustomAttribute<HarmonyPatchCategory>(), tweakCategory: t.GetCustomAttribute<TweakCategoryAttribute>()))
      .Where(t => t.patchCategory != null && t.tweakCategory != null)
      .Select(t => new ModConfigurationKey<bool>(t.patchCategory!.info.category, t.tweakCategory!.Description, computeDefault: () => t.tweakCategory.DefaultValue));

    foreach (var key in categoryKeys)
    {
      DebugFunc(() => $"Registering patch category {key.Name}...");
      patchCategoryKeys[key.Name] = key;
    }
  }

  public override void DefineConfiguration(ModConfigurationDefinitionBuilder builder)
  {
    if (builder is null)
    {
      throw new ArgumentNullException(nameof(builder), "builder is null.");
    }

    foreach (var key in patchCategoryKeys.Values)
    {
      DebugFunc(() => $"Adding configuration key for {key.Name}...");
      builder.Key(key);
    }
  }


  public override void OnEngineInit()
  {
    config = GetConfiguration()!; // todo: tired, fix
    config.OnThisConfigurationChanged += OnConfigChanged;

    InitCategories();

#if DEBUG
    HotReloader.RegisterForHotReload(this);
#endif
  }

  public static void InitCategories()
  {
    foreach (var (category, key) in patchCategoryKeys)
    {
      UpdatePatch(category, config!.GetValue(key));
    }
  }


#if DEBUG
  static void BeforeHotReload()
  {
    foreach (var category in patchCategoryKeys.Keys)
    {
      UpdatePatch(category, false);
    }
  }

  static void OnHotReload(ResoniteMod modInstance)
  {
    foreach (var (category, key) in patchCategoryKeys)
    {
      UpdatePatch(category, true);
    }
  }
#endif

  private static void UpdatePatch(string category, bool enabled)
  {
    try
    {

      if (enabled)
      {
        DebugFunc(() => $"Patching {category}...");
        harmony.PatchCategory(category);
      }
      else
      {
        DebugFunc(() => $"Unpatching {category}...");
        // harmony.UnpatchCategory(category);
        harmony.UnpatchAll(harmony.Id);
      }
    }
    catch (Exception e)
    {
      Error(e);
    }
  }

  private static void OnConfigChanged(ConfigurationChangedEvent change)
  {
    if (change.Key is ModConfigurationKey<bool> key)
    {
      UpdatePatch(key.Name, change.Config.GetValue(key));
    }
  }
}