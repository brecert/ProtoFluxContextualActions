using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using FrooxEngine;

namespace ProtoFluxContextualActions.Utils;

using PsuedoGenerics = IEnumerable<(Type Node, IEnumerable<Type> Types)>;

static class PsuedoGenericTypesHelper
{
  internal static readonly ConditionalWeakTable<World, PsuedoGenericTypes> WorldPsuedoGenericTypes = [];

  public static PsuedoGenericTypes GetPsuedoGenericTypesForWorld(this World world) =>
    WorldPsuedoGenericTypes.GetValue(world, (w) => new(w));

  // TODO: move these to tagging?
  // BinaryOperations must be kept in sync with BinaryOperationsMulti in order for zipping
  // operations to remain consistent between the two.
  public static PsuedoGenerics BinaryOperations(this PsuedoGenericTypes psuedoGenerics) =>
    [
      .. psuedoGenerics.AND,
      .. psuedoGenerics.OR,
      .. psuedoGenerics.NAND,
      .. psuedoGenerics.NOR,
      .. psuedoGenerics.XNOR,
      .. psuedoGenerics.XOR,
    ];

  public static PsuedoGenerics BinaryOperationsMulti(this PsuedoGenericTypes psuedoGenerics) =>
    [
      .. psuedoGenerics.AND_Multi,
      .. psuedoGenerics.OR_Multi,
      .. psuedoGenerics.NAND_Multi,
      .. psuedoGenerics.NOR_Multi,
      .. psuedoGenerics.XNOR_Multi,
      .. psuedoGenerics.XOR_Multi,
    ];

  public static PsuedoGenerics AvgGroup(this PsuedoGenericTypes psuedoGenerics) =>
    [
      .. psuedoGenerics.Avg,
      .. psuedoGenerics.AvgMulti,
    ];

  public static PsuedoGenerics PackingNodes(this PsuedoGenericTypes psuedoGenerics) =>
    [
      .. psuedoGenerics.Pack,
      .. psuedoGenerics.FromEuler,
      .. psuedoGenerics.PackRows,
      .. psuedoGenerics.PackColumns,
      .. psuedoGenerics.ComposeTRS,
      .. psuedoGenerics.PackTangentPoint,
    ];

  public static PsuedoGenerics UnpackingNodes(this PsuedoGenericTypes psuedoGenerics) =>
    [
      .. psuedoGenerics.Unpack,
      .. psuedoGenerics.EulerAngles,
      .. psuedoGenerics.UnpackRows,
      .. psuedoGenerics.UnpackColumns,
      // .. psuedoGenerics.ComposeTRS,
    ];

  public static PsuedoGenerics ComparisonNodes(this PsuedoGenericTypes psuedoGenerics) =>
    [
      .. psuedoGenerics.LessThan,
      .. psuedoGenerics.LessOrEqual,
      .. psuedoGenerics.GreaterThan,
      .. psuedoGenerics.GreaterOrEqual,
    ];
}