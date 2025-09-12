using System;
using System.Collections;
using System.Collections.Frozen;
using System.Collections.Generic;
using System.Linq;
using Elements.Core;
using ProtoFlux.Runtimes.Execution.Nodes.FrooxEngine.Input.Mouse;
using ProtoFlux.Runtimes.Execution.Nodes.FrooxEngine.Time;
using ProtoFlux.Runtimes.Execution.Nodes.FrooxEngine.Users.LocalScreen;
using ProtoFlux.Runtimes.Execution.Nodes.Math.Easing;

namespace ProtoFluxContextualActions.Tagging;

static class EasingGroups
{

  // todo: currently there's too many, page support or custom uix menus are needed
  public static readonly Type[] EasingInGroupFloat = [
    typeof(EaseInBounceFloat),
    typeof(EaseInCircularFloat),
    typeof(EaseInCubicFloat),
    typeof(EaseInElasticFloat),
    typeof(EaseInExponentialFloat),
    typeof(EaseInQuadraticFloat),
    typeof(EaseInQuarticFloat),
    typeof(EaseInQuinticFloat),
    typeof(EaseInReboundFloat),
    typeof(EaseInSineFloat),
  ];

  public static readonly Type[] EasingOutGroupFloat = [
    typeof(EaseOutBounceFloat),
    typeof(EaseOutCircularFloat),
    typeof(EaseOutCubicFloat),
    typeof(EaseOutElasticFloat),
    typeof(EaseOutExponentialFloat),
    typeof(EaseOutQuadraticFloat),
    typeof(EaseOutQuarticFloat),
    typeof(EaseOutQuinticFloat),
    typeof(EaseOutReboundFloat),
    typeof(EaseOutSineFloat),
  ];

  public static readonly Type[] EasingInOutGroupFloat = [
    typeof(EaseInOutBounceFloat),
    typeof(EaseInOutCircularFloat),
    typeof(EaseInOutCubicFloat),
    typeof(EaseInOutElasticFloat),
    typeof(EaseInOutExponentialFloat),
    typeof(EaseInOutQuadraticFloat),
    typeof(EaseInOutQuarticFloat),
    typeof(EaseInOutQuinticFloat),
    typeof(EaseInOutReboundFloat),
    typeof(EaseInOutSineFloat),
  ];

  public static readonly Type[] EasingInGroupDouble = [
    typeof(EaseInBounceDouble),
    typeof(EaseInCircularDouble),
    typeof(EaseInCubicDouble),
    typeof(EaseInElasticDouble),
    typeof(EaseInExponentialDouble),
    typeof(EaseInQuadraticDouble),
    typeof(EaseInQuarticDouble),
    typeof(EaseInQuinticDouble),
    typeof(EaseInReboundDouble),
    typeof(EaseInSineDouble),
  ];

  public static readonly Type[] EasingOutGroupDouble = [
    typeof(EaseOutBounceDouble),
    typeof(EaseOutCircularDouble),
    typeof(EaseOutCubicDouble),
    typeof(EaseOutElasticDouble),
    typeof(EaseOutExponentialDouble),
    typeof(EaseOutQuadraticDouble),
    typeof(EaseOutQuarticDouble),
    typeof(EaseOutQuinticDouble),
    typeof(EaseOutReboundDouble),
    typeof(EaseOutSineDouble),
  ];

  public static readonly Type[] EasingInOutGroupDouble = [
    typeof(EaseInOutBounceDouble),
    typeof(EaseInOutCircularDouble),
    typeof(EaseInOutCubicDouble),
    typeof(EaseInOutElasticDouble),
    typeof(EaseInOutExponentialDouble),
    typeof(EaseInOutQuadraticDouble),
    typeof(EaseInOutQuarticDouble),
    typeof(EaseInOutQuinticDouble),
    typeof(EaseInOutReboundDouble),
    typeof(EaseInOutSineDouble),
  ];

  public static bool ContainsNodeFloat(Type type) =>
    EasingInGroupFloat.Contains(type)
    || EasingOutGroupFloat.Contains(type)
    || EasingInOutGroupFloat.Contains(type);

  public static bool ContainsNodeDouble(Type type) =>
    EasingInGroupFloat.Contains(type)
    || EasingOutGroupFloat.Contains(type)
    || EasingInOutGroupFloat.Contains(type);

  public static IEnumerable<Type> GetEasingOfSameKindFloat(Type nodeType)
  {
    if (EasingInGroupFloat.FindIndex(n => n == nodeType) is var i and not -1)
    {
      yield return EasingInGroupFloat[i];
      yield return EasingOutGroupFloat[i];
      yield return EasingInOutGroupFloat[i];
    }
    else if (EasingOutGroupFloat.FindIndex(n => n == nodeType) is var i2 and not -1)
    {
      yield return EasingInGroupFloat[i2];
      yield return EasingOutGroupFloat[i2];
      yield return EasingInOutGroupFloat[i2];
    }
    else if (EasingInOutGroupFloat.FindIndex(n => n == nodeType) is var i3 and not -1)
    {
      yield return EasingInGroupFloat[i3];
      yield return EasingOutGroupFloat[i3];
      yield return EasingInOutGroupFloat[i3];
    }
  }

  public static IEnumerable<Type> GetEasingOfSameKindDouble(Type nodeType)
  {
    if (EasingInGroupDouble.FindIndex(n => n == nodeType) is var i and not -1)
    {
      yield return EasingInGroupDouble[i];
      yield return EasingOutGroupDouble[i];
      yield return EasingInOutGroupDouble[i];
    }
    else if (EasingOutGroupDouble.FindIndex(n => n == nodeType) is var i2 and not -1)
    {
      yield return EasingInGroupDouble[i2];
      yield return EasingOutGroupDouble[i2];
      yield return EasingInOutGroupDouble[i2];
    }
    else if (EasingInOutGroupDouble.FindIndex(n => n == nodeType) is var i3 and not -1)
    {
      yield return EasingInGroupDouble[i3];
      yield return EasingOutGroupDouble[i3];
      yield return EasingInOutGroupDouble[i3];
    }
  }
}