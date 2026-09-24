using Elements.Core;

using FrooxEngine;

using ProtoFlux.Core;

using ProtoFluxContextualActions.Extensions;
using ProtoFluxContextualActions.Patches;
using ProtoFluxContextualActions.Utils;

namespace ProtoFluxContextualActions.Types;

sealed partial class PsuedoTypeDefinition(string Prefix) : ITypeDefinition
{
  private BiDictionary<Type, Type[]> Registry;

  public void RegisterTypes(TypeManager worldTypes) =>
    Registry ??= PsuedoGenericUtils.GetProtoFluxNodes().Values
      .AsParallel()
      .Select(t => NodeUtils.ProtoFluxBindingMapping.GetValueOrDefault(t))
      .OfType<Type>()
      .Where(t => t.GetNiceTypeName().StartsWith(Prefix))
      .Select(t => (t, TryParseGenerics(t, worldTypes)))
      .OfType<(Type, Type[])>()
      .ToBiDictionary();

  public PsuedoType? TryCreateTypeFrom(Type type, TypeManager worldTypes)
  {
    // TODO: handle worldTypes.IsSupported so that types are per-world rather than global.
    RegisterTypes(worldTypes);

    // fast path, should always be reached unless in a world that defines new protoflux bindings (ie. with plugins)
    if (Registry.TryGetSecond(type, out var generics))
    {
      return new(type, this, generics);
    }

    if (TryParseGenerics(type, worldTypes) is { } parsedGenerics)
    {
      var matchedTypes = parsedGenerics.ToArray();
      Registry.Add(type, matchedTypes);
      return new(type, this, matchedTypes);
    }

    return null;
  }

  Type? ITypeDefinition.TryMakeGenericType(params Type[] typeArguments) =>
    Registry.FirstOrDefault(t => t.Second.SequenceEqual(typeArguments)).First;

  IType? ITypeDefinition.TryCreateTypeFrom(Type type, TypeManager worldTypes) =>
    TryCreateTypeFrom(type, worldTypes);

  private Type[]? TryParseGenerics(Type type, TypeManager worldTypes)
  {
    if (type.GetNiceTypeName().StartsWith(Prefix))
    {
      return type.Name[Prefix.Length..].Split('_')
        .Select(name => worldTypes.DecodeType(name) ?? worldTypes.DecodeType(name.ToLower()))
        .ToArray();
    }
    else
    {
      return null;
    }
  }

}
record PsuedoType(Type OriginalType, PsuedoTypeDefinition Definition, Type[] GenericArguments) : IType
{
  public Type? ResolvedType => OriginalType;
}
