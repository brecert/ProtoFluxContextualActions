using System;
using Elements.Core;

using FrooxEngine.ProtoFlux;

using static ProtoFluxContextualActions.Utils.PsuedoGenericUtils;
using HarmonyLib;
using System.Collections.Generic;
using System.Linq;
using ProtoFlux.Core;
using ProtoFlux.Runtimes.Execution.Nodes.Operators;
using ProtoFlux.Runtimes.Execution.Nodes.Math;
using ProtoFluxContextualActions.Utils;
using ProtoFlux.Runtimes.Execution.Nodes.Binary;
using ProtoFlux.Runtimes.Execution.Nodes.FrooxEngine.Operators;
using ProtoFlux.Runtimes.Execution.Nodes.Math.Quaternions;

namespace ProtoFluxContextualActions.Patches;


static partial class ContextualSelectionActionsPatch
{
  internal static IEnumerable<MenuItem> GeneralNumericOperationMenuItems(ProtoFluxElementProxy? target)
  {
    {
      // TODO: It's nice to have these work with any node, I think their precedence should be lower than manually specified ones and potentially hidden by default for many types that support but do not need, esp. comparison.
      //       When I'm more sure that Swapping won't world crash I think I can limit comparison to a single node and then swap to the right one as a sort of submenu?
      //       Feels a little weird though, ux is difficult. A custom uix menu could help.
      if (target != null)
      {
        Type? nodeType = null;
        var world = target.World;
        var psuedoGenericTypes = world.GetPsuedoGenericTypesForWorld();
        if (target is ProtoFluxOutputProxy { OutputType.Value: var outputType } && (outputType.IsUnmanaged() || typeof(ISphericalHarmonics).IsAssignableFrom(outputType)))
        {
          var coder = Traverse.Create(typeof(Coder<>).MakeGenericType(outputType));
          var isMatrix = outputType.IsMatrixType();
          var isQuaternion = outputType.IsQuaternionType();
          nodeType = outputType;
          // only handle values

          if (isQuaternion)
          {
            if (TryGetPsuedoGenericForType(world, "Pow_", outputType) is Type powType)
            {
              yield return new MenuItem(powType);
            }

            if (coder.Property<bool>("SupportsMul").Value)
            {
              yield return new MenuItem(typeof(ValueMul<>).MakeGenericType(outputType));
            }

            if (coder.Property<bool>("SupportsDiv").Value)
            {
              yield return new MenuItem(typeof(ValueDiv<>).MakeGenericType(outputType));
            }
          }
          else
          {
            if (coder.Property<bool>("SupportsAddSub").Value)
            {
              yield return new MenuItem(typeof(ValueAdd<>).MakeGenericType(outputType));
              yield return new MenuItem(typeof(ValueSub<>).MakeGenericType(outputType));
            }

            if (coder.Property<bool>("SupportsMul").Value)
            {
              yield return new MenuItem(typeof(ValueMul<>).MakeGenericType(outputType));
            }

            if (coder.Property<bool>("SupportsDiv").Value)
            {
              yield return new MenuItem(typeof(ValueDiv<>).MakeGenericType(outputType));
            }

            if (coder.Property<bool>("SupportsNegate").Value)
            {
              yield return new MenuItem(typeof(ValueNegate<>).MakeGenericType(outputType), group: "Math");
            }

            if (coder.Property<bool>("SupportsMod").Value)
            {
              yield return new MenuItem(typeof(ValueMod<>).MakeGenericType(outputType), group: "Math");
            }

            if (coder.Property<bool>("SupportsAbs").Value && !isMatrix)
            {
              yield return new MenuItem(typeof(ValueAbs<>).MakeGenericType(outputType), group: "Math");
            }

            if (coder.Property<bool>("SupportsComparison").Value)
            {
              yield return new MenuItem(typeof(ValueMax<>).MakeGenericType(outputType), group: "Comparisons");
              // yield return new MenuItem(typeof(ValueLessThan<>).MakeGenericType(outputType));
              // yield return new MenuItem(typeof(ValueLessOrEqual<>).MakeGenericType(outputType));
              // yield return new MenuItem(typeof(ValueGreaterThan<>).MakeGenericType(outputType));
              // yield return new MenuItem(typeof(ValueGreaterOrEqual<>).MakeGenericType(outputType));
              // yield return new MenuItem(typeof(ValueEquals<>).MakeGenericType(outputType));
              // yield return new MenuItem(typeof(ValueNotEquals<>).MakeGenericType(outputType));
            }

            if (coder.Property<bool>("SupportsAddSub").Value)
            {
              yield return new MenuItem(typeof(ValueInc<>).MakeGenericType(outputType), group: "Math");
              yield return new MenuItem(typeof(ValueOneMinus<>).MakeGenericType(outputType), group: "Math");
              yield return new MenuItem(typeof(ValueDelta<>).MakeGenericType(outputType), group: "Math/Time");
            }
            if (coder.Property<bool>("SupportsMul").Value)
            {
              yield return new MenuItem(typeof(ValueSquare<>).MakeGenericType(outputType), group: "Math");
              yield return new MenuItem(typeof(MulDeltaTime<>).MakeGenericType(outputType), group: "Math/Time");
            }
            if (coder.Property<bool>("SupportsDiv").Value)
            {
              yield return new MenuItem(typeof(ValueReciprocal<>).MakeGenericType(outputType), group: "Math");
            }
          }

          if (coder.Property<bool>("SupportsLerp").Value)
          {
            yield return new MenuItem(typeof(ValueLerp<>).MakeGenericType(outputType), group: "Math/Lerping");
          }
          if (coder.Property<bool>("SupportsSmoothLerp").Value)
          {
            yield return new MenuItem(typeof(ValueSmoothLerp<>).MakeGenericType(outputType), group: "Math/Lerping");
          }

          if (coder.Property<bool>("SupportsMinMax").Value)
          {
            yield return new MenuItem(typeof(ValueClamp<>).MakeGenericType(outputType), group: "Comparisons");
          }

          if (TryGetInverseNode(outputType, out var inverseNodeType))
          {
            yield return new MenuItem(inverseNodeType, group: "Math");
          }

          if (TryGetTransposeNode(outputType, out var transposeNodeType))
          {
            yield return new MenuItem(transposeNodeType, name: "Transpose");
          }

          // While not often used, masking is useful.
          if (psuedoGenericTypes.Mask.Any(n => n.Types.First() == outputType))
          {
            yield return new(psuedoGenericTypes.Mask.First(n => n.Types.First() == outputType).Node, group: "Comparisons");
          }

          if (psuedoGenericTypes.Round.Any(n => n.Types.First() == outputType))
          {
            yield return new(psuedoGenericTypes.Round.First(n => n.Types.First() == outputType).Node, group: "Math");
          }

          if (outputType == typeof(bool))
          {
            foreach (var node in psuedoGenericTypes.ZeroOne)
            {
              yield return new(node.Node, group: "Zero One");
            }
          }

          if (psuedoGenericTypes.Sin.Any(n => n.Types.First() == outputType))
          {
            yield return new(psuedoGenericTypes.Sin.First(n => n.Types.First() == outputType).Node, group: "Math");
          }

          if (outputType == typeof(float))
          {
            yield return new(typeof(Remap_Float), group: "Math");
          }
          if (outputType == typeof(double))
          {
            yield return new(typeof(Remap_Double), group: "Math");
          }

          if (nodeType == typeof(Half)) yield return new(typeof(HalfAsUShort), group: "Math/Binary");
          if (nodeType == typeof(float)) yield return new(typeof(FloatAsUInt), group: "Math/Binary");
          if (nodeType == typeof(double)) yield return new(typeof(DoubleAsULong), group: "Math/Binary");

          if (nodeType == typeof(ushort)) yield return new(typeof(UShortAsHalf), group: "Math/Binary");
          if (nodeType == typeof(uint)) yield return new(typeof(UIntAsFloat), group: "Math/Binary");
          if (nodeType == typeof(ulong)) yield return new(typeof(ULongAsDouble), group: "Math/Binary");

          if (nodeType == typeof(byte) || nodeType == typeof(ushort) || nodeType == typeof(uint) || nodeType == typeof(ulong))
          {
            if (nodeType == typeof(uint) || nodeType == typeof(ulong))
            {
              yield return new(psuedoGenericTypes.AND.First(n => n.Types.First() == outputType).Node, group: "Math/Binary");
              yield return new(psuedoGenericTypes.ShiftLeft.First(n => n.Types.First() == outputType).Node, group: "Math/Binary");
            }

            yield return new(psuedoGenericTypes.ExtractBits.First(n => n.Types.First() == outputType).Node, group: "Math/Binary");
          }

          if (psuedoGenericTypes.Pack.Any(t => t.Types.First().BaseVectorType(out _) == nodeType))
          {
            foreach (var node in psuedoGenericTypes.Pack.Where(t => t.Types.First().BaseVectorType(out _) == nodeType))
            {
              yield return new(node.Node, group: "Vectors");
            }
          }
          if (nodeType == typeof(float3))
          {
            yield return new(typeof(FromEuler_floatQ), group: "Vectors");
          }
        }
        if (target is ProtoFluxInputProxy { InputType.Value: var inputType } && (inputType.IsUnmanaged() || typeof(ISphericalHarmonics).IsAssignableFrom(inputType)))
        {
          nodeType = inputType;
          if (psuedoGenericTypes.ZeroOne.Any(n => n.Types.First() == nodeType))
          {
            yield return new(psuedoGenericTypes.ZeroOne.First(n => n.Types.First() == nodeType).Node, group: "Math");
          }

          if (nodeType == typeof(Half)) yield return new(typeof(UShortAsHalf), group: "Math/Binary");
          if (nodeType == typeof(float)) yield return new(typeof(UIntAsFloat), group: "Math/Binary");
          if (nodeType == typeof(double)) yield return new(typeof(ULongAsDouble), group: "Math/Binary");

          if (nodeType == typeof(ushort)) yield return new(typeof(HalfAsUShort), group: "Math/Binary");
          if (nodeType == typeof(uint)) yield return new(typeof(FloatAsUInt), group: "Math/Binary");
          if (nodeType == typeof(ulong)) yield return new(typeof(DoubleAsULong), group: "Math/Binary");

          if (nodeType == typeof(byte) || nodeType == typeof(ushort) || nodeType == typeof(uint) || nodeType == typeof(ulong))
          {

            yield return new(psuedoGenericTypes.ComposeBits.First(n => n.Types.First() == nodeType).Node, group: "Math/Binary");
          }

          if (psuedoGenericTypes.Unpack.Any(t => t.Types.First().BaseVectorType(out _) == nodeType))
          {
            foreach (var node in psuedoGenericTypes.Unpack.Where(t => t.Types.First().BaseVectorType(out _) == nodeType))
            {
              yield return new(node.Node, group: "Vectors");
            }
          }
        }
        if (nodeType != null)
        {
          // keeping this around *just in case* something ends up needing it.
          // though, i dont know what would actually go here, despite trying multiple times.
        }
      }
    }
  }
}