using HarmonyLib;

namespace ProtoFluxContextualActions.Attributes;

/// used for defining groups of patches that can be enabled or disabled to patch and unpatch the group of patches as a mod setting
[AttributeUsage(AttributeTargets.Class, AllowMultiple = false)]
internal sealed class PatchGroup(string name, string description, bool defaultValue = true, bool hidden = false) : HarmonyPatchCategory(category: name)
{
  public readonly string Description = description;
  public readonly bool DefaultValue = defaultValue;
  public readonly bool Hidden = hidden;
}
