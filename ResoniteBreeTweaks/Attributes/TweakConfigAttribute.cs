using System;

namespace BreeTweaks.Attributes;

[AttributeUsage(AttributeTargets.Class, AllowMultiple = false)]
internal sealed class TweakOptionAttribute(string name, string description) : Attribute
{
    public string Name { get; } = name;
    public string Description { get; } = description;
}