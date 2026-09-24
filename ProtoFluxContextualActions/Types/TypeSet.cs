using System.Diagnostics;

using Elements.Core;

using FrooxEngine;

using ProtoFluxContextualActions.Utils;

namespace ProtoFluxContextualActions.Types;

interface ITypeSet
{
  public IEnumerable<Type> GetMatchingTypes(Type type, TypeManager worldTypes);
}

class TypeSet : HashSet<ITypeDefinition>, ITypeSet
{
  public IEnumerable<Type> TryMakeGeneric(params Type[] typeArguments) =>
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

class SystemTypeSet : HashSet<Type>, ITypeSet
{
  public IEnumerable<Type> GetMatchingTypes(Type type, TypeManager worldTypes)
  {
    foreach (var t in this)
    {
      if (TypeUtils.MatchInterface(type, t, out var matchedType))
      {
        foreach (var ty in this)
        {
          if (matchedType.IsGenericType)
          {
            if (ty.TryMakingGenericTypeFrom(matchedType) is Type filledType)
            {
              yield return filledType;
            }
          }
          else
          {
            yield return ty;
          }
        }
        break;
      }
    }
  }
}

static class ContextActionTypeSetExtensions
{
  public static IEnumerable<Type> GetMatchingTypes(this ITypeSet typeSet, Patches.ContextualSwapActionsPatch.ContextualContext context) =>
    typeSet.GetMatchingTypes(context.NodeType, context.World.Types);
}
