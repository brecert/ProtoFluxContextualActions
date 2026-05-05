using System;
using System.Collections.Generic;
using Elements.Core;
using ProtoFlux.Runtimes.Execution.Nodes;
using ProtoFlux.Runtimes.Execution.Nodes.FrooxEngine.Transform;
using ProtoFlux.Runtimes.Execution.Nodes.FrooxEngine.Utility;
using ProtoFluxContextualActions.Utils;

namespace ProtoFluxContextualActions.Patches;

static partial class ContextualSwapActionsPatch
{
  static readonly HashSet<Type> TransformVectorGroup = [
    typeof(TransformPoint),
    typeof(TransformDirection),
    typeof(TransformVector),
    typeof(TransformScale),
  ];
  static readonly BiDictionary<Type, Type> VectorGlobalLocalGroup = new() {
    { typeof(GlobalPointToLocal), typeof(LocalPointToGlobal) },
    { typeof(GlobalDirectionToLocal), typeof(LocalDirectionToGlobal) },
    { typeof(GlobalVectorToLocal), typeof(LocalVectorToGlobal) },
    { typeof(GlobalScaleToLocal), typeof(LocalScaleToGlobal) },
    { typeof(GlobalRotationToLocal), typeof(LocalRotationToGlobal) },
  };
  static readonly BiDictionary<Type, Type> GlobalVectorTransformSwapGroup = new() {
    { typeof(GlobalPointToLocal), typeof(TransformPoint) },
    { typeof(GlobalDirectionToLocal), typeof(TransformDirection) },
    { typeof(GlobalVectorToLocal), typeof(TransformVector) },
    { typeof(GlobalScaleToLocal), typeof(TransformScale) },
    { typeof(GlobalRotationToLocal), typeof(TransformRotation) },
  };
  static readonly BiDictionary<Type, Type> LocalVectorTransformSwapGroup = new() {
    { typeof(LocalPointToGlobal), typeof(TransformPoint) },
    { typeof(LocalDirectionToGlobal), typeof(TransformDirection) },
    { typeof(LocalVectorToGlobal), typeof(TransformVector) },
    { typeof(LocalScaleToGlobal), typeof(TransformScale) },
    { typeof(LocalRotationToGlobal), typeof(TransformRotation) },
  };

  internal static IEnumerable<MenuItem> TransformVectorGroupItems(ContextualContext context)
  {
    if (TransformVectorGroup.Contains(context.NodeType))
    {
      foreach (var vecNode in TransformVectorGroup)
      {
        yield return new MenuItem(vecNode, connectionTransferType: ConnectionTransferType.ByIndexLossy);
      }
    }
    if (TryGetSwap(VectorGlobalLocalGroup, context.NodeType, out Type match)) yield return new(match);
    if (TryGetSwap(GlobalVectorTransformSwapGroup, context.NodeType, out match)) yield return new(match);
    // this one may not be needed, but it could speed some things up a little.
    if (TryGetSwap(LocalVectorTransformSwapGroup, context.NodeType, out match)) yield return new(match);
  }
}