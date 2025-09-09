using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Runtime.CompilerServices;
using FrooxEngine;
using FrooxEngine.ProtoFlux;
using HarmonyLib;
using Mono.Cecil.Cil;
using ProtoFlux.Core;
using ProtoFluxContextualActions.Patches;

namespace ProtoFluxContextualActions.Utils;

public static class PsuedoGenericUtils
{
  public static IEnumerable<(Type Node, IEnumerable<Type> Types)> MapPsuedoGenericsToGenericTypes(World world, string startingWith) =>
    GetProtoFluxNodes().Values
      .Select(t => (name: t.GetNiceTypeName(), type: t))
      .Where(a => a.name.StartsWith(startingWith) && !a.type.IsGenericType)
      .Select(a => (a.type, ParseUnderscoreGenerics(world, a.name[startingWith.Length..])))
      // skip non matching
      .Where(a => a.Item2.All(t => t != null));

  public static Type? TryGetPsuedoGenericForType(World world, string startingWith, params Type[] types)
  {
    var (node, _) = MapPsuedoGenericsToGenericTypes(world, startingWith).FirstOrDefault(n => n.Types.SequenceEqual(types));
    if (node is not null)
    {
      return NodeUtils.ProtoFluxBindingMapping[node];
    }
    else
    {
      return null;
    }
  }

  static IEnumerable<Type> ParseUnderscoreGenerics(World world, string generics) =>
      generics.Split('_').Select(name => world.Types.DecodeType(name.ToLower()) ?? world.Types.DecodeType(name));

  static Dictionary<string, Type> GetProtoFluxNodes() =>
    Traverse.Create(typeof(ProtoFluxHelper)).Field<Dictionary<string, Type>>("protoFluxNodes").Value;
}