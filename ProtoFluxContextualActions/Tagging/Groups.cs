using System;
using System.Collections.Frozen;
using ProtoFlux.Runtimes.Execution.Nodes.FrooxEngine.Time;

namespace ProtoFluxContextualActions.Tagging;

static class Groups
{
  public static FrozenSet<Type> WorldTimeFloatGroup = [
    typeof(WorldTimeFloat),
    typeof(WorldTime2Float),
    typeof(WorldTime10Float),
    typeof(WorldTimeTenthFloat),
  ];

  public static FrozenSet<Type> WorldTimeDoubleGroup = [
    typeof(WorldTimeDouble),
  ];

  public static FrozenSet<Type> WorldTimeSwapGroup = [
    typeof(WorldTimeFloat),
    typeof(WorldTimeDouble),
  ];
}