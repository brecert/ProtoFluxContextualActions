using System;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using Elements.Core;

namespace ProtoFluxContextualActions.Utils;

static class TypeUtils
{
  public static Type? TryMakeGenericType(this Type type, params Type[] typeArguments)
  {
    try
    {
      var genericType = type.MakeGenericType(typeArguments);
      if (genericType.IsValidGenericType(validForInstantiation: true))
      {
        return genericType;
      }
      return null;
    }
    catch
    {
      return null;
    }
  }

  public static bool TryGetGenericTypeDefinition(this Type type, [NotNullWhen(true)] out Type? genericTypeDefinition)
  {
    if (type.IsGenericType)
    {
      genericTypeDefinition = type.GetGenericTypeDefinition();
      return true;
    }
    genericTypeDefinition = null;
    return false;
  }

  public static bool MatchesType(Type match, Type type)
  {
    if (match == type) return true;
    if (match.IsGenericTypeDefinition && type.IsGenericType)
    {
      return match == type.GetGenericTypeDefinition();
    }
    return false;
  }

  public static bool MatchInterface(Type type, Type interfaceType, [NotNullWhen(true)] out Type? matchedType)
  {
    if (type == interfaceType)
    {
      matchedType = type;
      return true;
    }

    if (interfaceType.IsGenericTypeDefinition)
    {
      if (type.IsGenericType && interfaceType == type.GetGenericTypeDefinition())
      {
        matchedType = type;
      }
      else
      {
        matchedType = type.FindInterfaces((t, _) => t.IsGenericType && interfaceType == t.GetGenericTypeDefinition(), null).FirstOrDefault();
      }
    }
    else
    {
      matchedType = type.FindInterfaces((t, _) => interfaceType == t, null).FirstOrDefault();
    }

    return matchedType != null;
  }
}
