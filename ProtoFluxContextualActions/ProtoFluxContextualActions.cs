using System.Reflection;

using Elements.Core;

using HarmonyLib;

using ProtoFluxContextualActions.Attributes;

using ResoniteModLoader;

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

  /// configuration keys that control if patches are enabled/disabled
  private static readonly Dictionary<string, ModConfigurationKey<bool>> PatchGroupKeys = [];

  [AutoRegisterConfigKey]
  private static readonly ModConfigurationKey<bool> forceExplicitCasts = new("Make Casts Explicit", "Whether connections between wires that are not the same type should have an explicit cast, rather than having an implicit cast.", () => false);
  internal static bool ExplicitCasts => forceExplicitCasts.Value;

  [AutoRegisterConfigKey]
  private static readonly ModConfigurationKey<bool> showUnsupportedActions = new("Show Unsupported Actions", "Whether to show actions that may have potentially undefined, or unsupported behavior. This will hide refhack related actions for example.", () => false);
  internal static bool ShouldDisplayUnsupportedActions => showUnsupportedActions.Value;

  [AutoRegisterConfigKey]
  private static readonly ModConfigurationKey<bool> showBackButton = new("Show Back Button", "Whether to show the back button in the submenus.", () => false);
  internal static bool ShouldDisplayBackButton => showBackButton.Value;

  [AutoRegisterConfigKey]
  private static readonly ModConfigurationKey<int> maxItemsPerPage = new("Max Items Per Page", "The maximum amount of items per page", () => 10);
  internal static int MaxItemsPerPage => maxItemsPerPage.Value;

  [AutoRegisterConfigKey]
  private static readonly ModConfigurationKey<bool> defaultActionOnPrimaryRelease = new("Default Action On Primary Release", "If a display/input should be created when primary is released", () => false);
  internal static bool ShouldDoDefaultActionOnPrimaryRelease => defaultActionOnPrimaryRelease.Value;

  [AutoRegisterConfigKey]
  private static readonly ModConfigurationKey<dummy> _advancedCategory = new("_AdvancedCategory", "<b>Advanced Settings</b>");

  [AutoRegisterConfigKey]
  private static readonly ModConfigurationKey<bool> tryFixFlick = new("Try Fix Context Flick", "If the context menu should attempt to fix flicking.", () => true);
  internal static bool ShouldTryFixFlick => tryFixFlick.Value;

  [AutoRegisterConfigKey]
  private static readonly ModConfigurationKey<bool> tryKeepContextPosition = new("Try Keep Context Menu Position", "If the context menu should attempt to stay in the same position.", () => false);
  internal static bool ShouldTryKeepContextPosition => tryKeepContextPosition.Value;

  [AutoRegisterConfigKey]
  private static readonly ModConfigurationKey<bool> fluxStructureRelays = new("Structure Relays", "If \"Flux Structures\" should contain relays.", () => true);
  internal static bool ShouldUseRelays => fluxStructureRelays.Value;

  [AutoRegisterConfigKey]
  private static readonly ModConfigurationKey<int> fluxStructureReleaseUpdates = new("Structure Release Updates", "How many updates to allow for structures to be 'released' from the 'global grabber'.", () => 240);
  internal static int StructureReleaseUpdates => fluxStructureReleaseUpdates.Value;

  // [AutoRegisterConfigKey]
  private static readonly ModConfigurationKey<MenuVisual> currentMenuVisual = new("Current Menu Visual", "The visual to use when rendering a menu.\t<b><color=hero.red>NOTE: No other visuals exist currently!</color></b> This setting can be ignored for now.", () => MenuVisual.ContextMenu);
  internal static MenuVisual MenuVisual => currentMenuVisual.Value;

  static ProtoFluxContextualActions()
  {
    Debug($"Static Initializing {nameof(ProtoFluxContextualActions)}...");

    var types = AccessTools.GetTypesFromAssembly(ModAssembly);

    PatchGroupKeys = types
      .Select(t => t.GetCustomAttribute<PatchGroup>())
      .OfType<PatchGroup>()
      .Select(t => new ModConfigurationKey<bool>(t.info.category, t.Description, () => t.DefaultValue, internalAccessOnly: t.Hidden))
      .ToDictionary(k => k.Name);
  }

  public override void DefineConfiguration(ModConfigurationDefinitionBuilder builder)
  {
    builder.Key(new ModConfigurationKey<dummy>("_PatchesCategory", "Patches"));
    foreach (var key in PatchGroupKeys.Values)
    {
      Debug($"Adding configuration key for {key.Name}...");
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
  internal static void BeforeHotReload()
  {
    harmony.UnpatchAll(HarmonyId);
    PsuedoGenericTypesHelper.WorldPsuedoGenericTypes.Clear();
  }

  internal static void OnHotReload(ResoniteMod modInstance)
  {
    PatchCategories();
    harmony.PatchAllUncategorized(ModAssembly);
  }

  internal static void UnpatchCategories()
  {
    foreach (var category in PatchGroupKeys.Keys)
    {
      harmony.UnpatchCategory(ModAssembly, category);
    }
  }
#endif

  internal static void PatchCategories()
  {
    foreach (var (category, key) in PatchGroupKeys)
    {
      if (Config?.GetValue(key) ?? true) // enable if fail?
      {
        harmony.PatchCategory(ModAssembly, category);
      }
    }
  }

  private static void OnConfigChanged(ConfigurationChangedEvent change)
  {
    if (change.Key is ModConfigurationKey<bool> { Name: var category } key && PatchGroupKeys.ContainsKey(category))
    {
      if (change.Config.GetValue(key))
      {
        Debug($"Patching {category}...");
        harmony.PatchCategory(category);
      }
      else
      {
        Debug($"Unpatching {category}...");
        harmony.UnpatchCategory(category);
      }
    }
  }
}
