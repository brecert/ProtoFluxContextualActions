using System;
using System.Collections.Generic;
using ProtoFlux.Runtimes.Execution.Nodes.Math.Random;

namespace ProtoFluxContextualActions.Patches;

static partial class ContextualSwapActionsPatch
{
  static readonly HashSet<Type> RandomPoint3DGroup = [
    typeof(RandomPointInSphere),
    typeof(RandomPointOnSphere),
    typeof(RandomPointInCube),
    typeof(RandomPointOnCube),

    typeof(RandomPointInCone),
    typeof(RandomPointOnCone),
  ];

  static readonly HashSet<Type> RandomPoint2DGroup = [
    typeof(RandomPointInCircle),
    typeof(RandomPointOnCircle),
    typeof(RandomPointInSquare),
    typeof(RandomPointOnSquare),
  ];

  internal static IEnumerable<MenuItem> RandomPointGroupItems(ContextualContext context)
  {
    if (RandomPoint3DGroup.Contains(context.NodeType))
    {
      foreach (var match in RandomPoint3DGroup)
      {
        yield return new MenuItem(match);
      }
    }
    if (RandomPoint2DGroup.Contains(context.NodeType))
    {
      foreach (var match in RandomPoint2DGroup)
      {
        yield return new MenuItem(match);
      }
    }
  }
}