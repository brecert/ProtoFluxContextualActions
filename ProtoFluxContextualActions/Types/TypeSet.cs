using System.Diagnostics;

using Elements.Core;

using FrooxEngine;

namespace ProtoFluxContextualActions.Types;

interface ITypeSet
{
  public IEnumerable<Type> GetMatchingTypes(Type type, TypeManager worldTypes);
}

class TypeSet : HashSet<ITypeDefinition>, ITypeSet
{
  public IEnumerable<Type> TryMakeGeneric(params Type[]? typeArguments) =>
    this.Select(t => t.TryMakeGenericType(typeArguments)).OfType<Type>();

  public IEnumerable<IType> TryCreateTypeFrom(Type type, TypeManager worldTypes) =>
    this.Select(t => t.TryCreateTypeFrom(type, worldTypes)).OfType<IType>();

  public IEnumerable<Type> Matches(Type type, TypeManager worldTypes) =>
    TryCreateTypeFrom(type, worldTypes)
      .Select(t => t.ResolvedType)
      .OfType<Type>();

  public IEnumerable<Type> GetMatchingTypes(Type type, TypeManager worldTypes)
  {
    if (TryCreateTypeFrom(type, worldTypes).FirstOrDefault() is IType matched)
    {
      return TryMakeGeneric(matched.GenericArguments);
    }
    return [];
  }
}

static class ContextActionTypeSetExtensions
{
  public static IEnumerable<Type> GetMatchingTypes(this ITypeSet typeSet, Patches.ContextualSwapActionsPatch.ContextualContext context) =>
    typeSet.GetMatchingTypes(context.NodeType, context.World.Types);
}
