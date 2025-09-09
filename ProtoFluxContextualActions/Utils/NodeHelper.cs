using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using FrooxEngine.ProtoFlux;
using HarmonyLib;
using ProtoFlux.Runtimes.Execution.Nodes.Enums;

namespace ProtoFluxContextualActions.Patches;


internal static class NodeUtils
{
  public static readonly Dictionary<Type, Type> EnumToNumberTypeMap = new()
    {
        {typeof(byte), typeof(EnumToByte<>)},
        {typeof(int), typeof(EnumToInt<>)},
        {typeof(long), typeof(EnumToLong<>)},
        {typeof(sbyte), typeof(EnumToSbyte<>)},
        {typeof(short), typeof(EnumToShort<>)},
        {typeof(uint), typeof(EnumToUint<>)},
        {typeof(ulong), typeof(EnumToUlong<>)},
        {typeof(ushort), typeof(EnumToUshort<>)},
    };

  public static readonly Dictionary<Type, Type> NumberToEnumTypeMap = new()
    {
        {typeof(byte), typeof(ByteToEnum<>)},
        {typeof(int), typeof(IntToEnum<>)},
        {typeof(long), typeof(LongToEnum<>)},
        {typeof(sbyte),typeof(SbyteToEnum<>)},
        {typeof(short),typeof(ShortToEnum<>)},
        {typeof(uint), typeof(UintToEnum<>)},
        {typeof(ulong), typeof(UlongToEnum<>)},
        {typeof(ushort), typeof(UshortToEnum<>)},
    };

  public static readonly Dictionary<Type, Type> TryEnumToNumberTypeMap = new()
    {
        {typeof(byte), typeof(TryEnumToByte<>)},
        {typeof(int), typeof(TryEnumToInt<>)},
        {typeof(long), typeof(TryEnumToLong<>)},
        {typeof(sbyte), typeof(TryEnumToSbyte<>)},
        {typeof(short), typeof(TryEnumToShort<>)},
        {typeof(uint), typeof(TryEnumToUint<>)},
        {typeof(ulong), typeof(TryEnumToUlong<>)},
        {typeof(ushort), typeof(TryEnumToUshort<>)},
    };

  public static readonly Dictionary<Type, Type> TryNumberToEnumTypeMap = new()
    {
        {typeof(byte), typeof(TryByteToEnum<>)},
        {typeof(int), typeof(TryIntToEnum<>)},
        {typeof(long), typeof(TryLongToEnum<>)},
        {typeof(sbyte), typeof(TrySbyteToEnum<>)},
        {typeof(short), typeof(TryShortToEnum<>)},
        {typeof(uint), typeof(TryUintToEnum<>)},
        {typeof(ulong), typeof(TryUlongToEnum<>)},
        {typeof(ushort), typeof(UshortToEnum<>)},
    };

  public static bool TryGetEnumToNumberNode(Type enumType, [MaybeNullWhen(false)] out Type type) => EnumToNumberTypeMap.TryGetValue(enumType, out type);

  public static bool TryGetNumberToEnumNode(Type enumType, [MaybeNullWhen(false)] out Type type) => NumberToEnumTypeMap.TryGetValue(enumType, out type);

  public static readonly Dictionary<Type, Type> ProtoFluxBindingMapping =
    Traverse.Create(typeof(ProtoFluxHelper)).Field<Dictionary<Type, Type>>("protoFluxToBindingMapping").Value.ToDictionary(a => a.Value, a => a.Key);
}