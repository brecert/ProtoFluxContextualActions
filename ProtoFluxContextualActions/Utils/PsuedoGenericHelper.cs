using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Runtime.CompilerServices;
using FrooxEngine;
using FrooxEngine.ProtoFlux;
using HarmonyLib;
using ProtoFlux.Core;

namespace ProtoFluxContextualActions.Utils;

public static class PsuedoGenericHelper
{
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

  public static IEnumerable<(Type Node, IEnumerable<Type> Types)> MapPsuedoGenericsToGenericTypes(World world, string startingWith) =>
    GetProtoFluxNodes().Values
      .Select(t => (name: t.GetNiceTypeName(), type: t))
      .Where(a => a.name.StartsWith(startingWith) && !a.type.IsGenericType)
      .Select(a => (a.type, ParseUnderscoreGenerics(world, a.name[startingWith.Length..])))
      // skip non matching
      .Where(a => a.Item2.All(t => t != null));

  static IEnumerable<Type> ParseUnderscoreGenerics(World world, string generics) =>
    generics.Split('_').Select(name => world.Types.DecodeType(name.ToLower()) ?? world.Types.DecodeType(name));

  static Dictionary<string, Type> GetProtoFluxNodes() =>
    Traverse.Create(typeof(ProtoFluxHelper)).Field<Dictionary<string, Type>>("protoFluxNodes").Value;
}