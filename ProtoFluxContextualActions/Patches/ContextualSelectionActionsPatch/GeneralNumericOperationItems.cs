using Elements.Core;

using FrooxEngine.ProtoFlux;

using HarmonyLib;

using ProtoFlux.Core;
using ProtoFlux.Runtimes.Execution.Nodes.Binary;
using ProtoFlux.Runtimes.Execution.Nodes.FrooxEngine.Operators;
using ProtoFlux.Runtimes.Execution.Nodes.Math;
using ProtoFlux.Runtimes.Execution.Nodes.Operators;

using ProtoFluxContextualActions.Utils;

using static ProtoFluxContextualActions.Utils.PsuedoGenericUtils;

namespace ProtoFluxContextualActions.Patches;


static partial class ContextualSelectionActionsPatch
{
  internal static IEnumerable<MenuItem> GeneralNumericOperationMenuItems(ProtoFluxElementProxy? target)
  {
    {
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
              yield return new(powType);
            }

            if (coder.Property<bool>("SupportsMul").Value)
            {
              yield return new(typeof(ValueMul<>).MakeGenericType(outputType));
            }

            if (coder.Property<bool>("SupportsDiv").Value)
            {
              yield return new(typeof(ValueDiv<>).MakeGenericType(outputType));
            }
          }
          else
          {
            if (coder.Property<bool>("SupportsAddSub").Value)
            {
              yield return new(typeof(ValueAdd<>).MakeGenericType(outputType));
              yield return new(typeof(ValueSub<>).MakeGenericType(outputType));
            }

            if (coder.Property<bool>("SupportsMul").Value)
            {
              yield return new(typeof(ValueMul<>).MakeGenericType(outputType));
            }

            if (coder.Property<bool>("SupportsDiv").Value)
            {
              yield return new(typeof(ValueDiv<>).MakeGenericType(outputType));
            }

            if (coder.Property<bool>("SupportsNegate").Value)
            {
              yield return new(typeof(ValueNegate<>).MakeGenericType(outputType), group: "Math");
            }

            if (coder.Property<bool>("SupportsMod").Value)
            {
              yield return new(typeof(ValueMod<>).MakeGenericType(outputType), group: "Math");
            }

            if (coder.Property<bool>("SupportsAbs").Value && !isMatrix)
            {
              yield return new(typeof(ValueAbs<>).MakeGenericType(outputType), group: "Math");
            }

            if (coder.Property<bool>("SupportsComparison").Value)
            {
              yield return new(typeof(ValueMax<>).MakeGenericType(outputType), group: "Comparisons");
              // yield return new(typeof(ValueLessThan<>).MakeGenericType(outputType));
              // yield return new(typeof(ValueLessOrEqual<>).MakeGenericType(outputType));
              // yield return new(typeof(ValueGreaterThan<>).MakeGenericType(outputType));
              // yield return new(typeof(ValueGreaterOrEqual<>).MakeGenericType(outputType));
              // yield return new(typeof(ValueEquals<>).MakeGenericType(outputType));
              // yield return new(typeof(ValueNotEquals<>).MakeGenericType(outputType));
            }

            if (coder.Property<bool>("SupportsAddSub").Value)
            {
              yield return new(typeof(ValueInc<>).MakeGenericType(outputType), group: "Math");
              yield return new(typeof(ValueOneMinus<>).MakeGenericType(outputType), group: "Math");
              yield return new(typeof(ValueDelta<>).MakeGenericType(outputType), group: "Math/Time");
            }
            if (coder.Property<bool>("SupportsMul").Value)
            {
              yield return new(typeof(ValueSquare<>).MakeGenericType(outputType), group: "Math");
              yield return new(typeof(MulDeltaTime<>).MakeGenericType(outputType), group: "Math/Time");
            }
            if (coder.Property<bool>("SupportsDiv").Value)
            {
              yield return new(typeof(ValueReciprocal<>).MakeGenericType(outputType), group: "Math");
            }
          }

          if (coder.Property<bool>("SupportsLerp").Value && outputType != typeof(bool))
          {
            yield return new(typeof(ValueLerp<>).MakeGenericType(outputType), group: "Math/Lerping");
          }
          if (coder.Property<bool>("SupportsSmoothLerp").Value)
          {
            yield return new(typeof(ValueSmoothLerp<>).MakeGenericType(outputType), group: "Math/Lerping");
          }
          if (psuedoGenericTypes.PackTangentPoint2.Any(t => t.Types.First() == outputType))
          {
            var packTangentNode = psuedoGenericTypes.PackTangentPoint2.First(t => t.Types.First() == outputType).Node;

            yield return new(packTangentNode, group: "Math/Lerping");
          }

          if (coder.Property<bool>("SupportsMinMax").Value)
          {
            yield return new(typeof(ValueClamp<>).MakeGenericType(outputType), group: "Comparisons");
          }

          if (TryGetInverseNode(outputType, out var inverseNodeType))
          {
            yield return new(inverseNodeType, group: "Math");
          }

          if (TryGetTransposeNode(outputType, out var transposeNodeType))
          {
            yield return new(transposeNodeType, name: "Transpose");
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

          if (psuedoGenericTypes.Pack.Any(t => t.Types.First().BaseVectorType(out var isVec) == nodeType && isVec))
          {
            foreach (var node in psuedoGenericTypes.Pack.Where(t => t.Types.First().BaseVectorType(out var isVec) == nodeType && isVec))
            {
              yield return new(node.Node, group: "Vectors");
            }
          }

          if (psuedoGenericTypes.Distance.Any(t => t.Types.First() == nodeType))
          {
            var isSingle = nodeType == typeof(float) || nodeType == typeof(double);
            yield return new(psuedoGenericTypes.Distance.First(t => t.Types.First() == nodeType).Node, group: isSingle ? "Math" : "Vectors");
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

          if (psuedoGenericTypes.Unpack.Any(t => t.Types.First().BaseVectorType(out var isVec) == nodeType && isVec))
          {
            foreach (var node in psuedoGenericTypes.Unpack.Where(t => t.Types.First().BaseVectorType(out var isVec) == nodeType && isVec))
            {
              yield return new(node.Node, group: "Vectors");
            }
          }
        }
      }
    }
  }
}
