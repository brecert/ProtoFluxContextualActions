using Elements.Core;

using FrooxEngine;

using ProtoFlux.Core;

namespace ProtoFluxContextualActions.Types;

record PsuedoTypeDefinition(string Prefix) : ITypeDefinition
{
  private readonly BiDictionary<Type, Type[]> Registry = [];

  public bool TryRegisterType(Type type, TypeManager worldTypes)
  {
    if (TryParseGenerics(type, worldTypes) is { } generics)
    {
      Registry.Add(type, generics);
      return true;
    }
    return false;
  }

  public PsuedoType? TryCreateTypeFrom(Type type, TypeManager worldTypes)
  {
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
    Registry.TryGetFirst(typeArguments, out var type) ? type : null;

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
record PsuedoType(Type OriginalType, PsuedoTypeDefinition Definition, Type[] Generics) : IType
{

}
