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

  public override string Name => ModAssembly.GetCustomAttribute<AssemblyTitleAttribute>().Title;
  public override string Author => ModAssembly.GetCustomAttribute<AssemblyCompanyAttribute>().Company;
  public override string Version => ModAssembly.GetCustomAttribute<AssemblyInformationalVersionAttribute>().InformationalVersion;
  public override string Link => ModAssembly.GetCustomAttributes<AssemblyMetadataAttribute>().First(meta => meta.Key == "RepositoryUrl").Value;

  internal static string HarmonyId => $"dev.bree.{ModAssembly.GetName()}";

  private static readonly Harmony harmony = new(HarmonyId);

  private static ModConfiguration? config;

  private static readonly Dictionary<string, ModConfigurationKey<bool>> patchCategoryKeys = [];
  // private static readonly Dictionary<ModConfigurationKey, FieldInfo> patchOptionKeys = [];

  static ProtoFluxContextualActions()
  {
    DebugFunc(() => $"Static Initializing {nameof(ProtoFluxContextualActions)}...");

    var types = AccessTools.GetTypesFromAssembly(ModAssembly);

    var categoryKeys = from t in types
                       select (t.GetCustomAttribute<HarmonyPatchCategory>(), t.GetCustomAttribute<TweakCategoryAttribute>()) into t
                       where t.Item1 is not null && t.Item2 is not null
                       select new ModConfigurationKey<bool>(t.Item1.info.category, t.Item2.Description, computeDefault: () => t.Item2.DefaultValue);

    foreach (var key in categoryKeys)
    {
      DebugFunc(() => $"Registering patch category {key.Name}...");
      patchCategoryKeys[key.Name] = key;
    }

    // var configFields = types
    //   .Where(t => t.IsDefined(typeof(HarmonyPatchCategory)))
    //   .SelectMany(AccessTools.GetDeclaredFields)
    //   .Where(f => f.IsDefined(typeof(TweakOptionAttribute)));

    // foreach (var field in configFields)
    // {
    //   DebugFunc(() => $"Registering patch config value {field.Name}...");
    //   var configValue = field.GetCustomAttribute<TweakOptionAttribute>();
    //   var defaultValue = field.GetCustomAttribute<DefaultValueAttribute>();

    //   var key = typeof(ModConfigurationKey<>)
    //     .MakeGenericType(field.FieldType)
    //     .GetConstructor([typeof(string), typeof(string), typeof(Func<>).MakeGenericType(field.FieldType)])
    //     .Invoke([configValue.Name, configValue.Description, defaultValue is not null ? () => defaultValue.Value : null]) as ModConfigurationKey;

    //   patchOptionKeys[key] = field;
    // }
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

  public void InitCategories()
  {
    foreach (var category in patchCategoryKeys.Keys)
    {
      UpdatePatch(category, true);
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
    foreach (var category in patchCategoryKeys.Keys)
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
        harmony.PatchCategory(category.ToString());
      }
      else
      {
        DebugFunc(() => $"Unpatching {category}...");
        harmony.UnpatchAll(HarmonyId);
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