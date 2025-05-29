using System;
using System.Linq;
using Elements.Core;
using HarmonyLib;

namespace ProtoFluxContextualActions.Utils;

static class TypeUtils
{
  public static bool MatchesType(Type match, Type type)
  {
    if (match == type) return true;
    if (match.IsGenericTypeDefinition && type.IsGenericType)
    {
      return match == type.GetGenericTypeDefinition();
    }
    return false;
  }

  public static bool MatchInterface(Type type, Type interfaceType, /* [NotNullWhen(true)] */ out Type? matchedType)
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
