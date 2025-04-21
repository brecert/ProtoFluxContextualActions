using System;

namespace ProtoFluxContextualActions.Attributes;

[AttributeUsage(AttributeTargets.Class, AllowMultiple = false)]
internal sealed class TweakCategoryAttribute(string description, bool defaultValue = true) : Attribute
{
  public string Description { get; } = description;
  public bool DefaultValue { get; } = defaultValue;
}