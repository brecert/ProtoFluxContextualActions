using System;
using System.Collections.Generic;
using Elements.Core;
using ProtoFlux.Runtimes.Execution.Nodes.Color;
using ProtoFlux.Runtimes.Execution.Nodes.FrooxEngine.Slots;

namespace ProtoFluxContextualActions.Patches;

static partial class ContextualSwapActionsPatch
{

  static readonly HashSet<Type> ColorRGBAGroup = [
    typeof(ColorXSetRed),
    typeof(ColorXSetGreen),
    typeof(ColorXSetBlue),
    typeof(ColorXSetAlpha),

    typeof(ColorXAddRedHDR),
    typeof(ColorXAddGreenHDR),
    typeof(ColorXAddBlueHDR),
    typeof(ColorXAddAlpha),

    typeof(ColorXMulRed),
    typeof(ColorXMulGreen),
    typeof(ColorXMulBlue),
    typeof(ColorXMulAlpha),
  ];

  static readonly HashSet<Type> ColorHSVGroup = [
    typeof(ColorXSetHue),
    typeof(ColorXSetSaturation),
    typeof(ColorXSetValue),

    typeof(ColorXAddHue),
    typeof(ColorXAddSaturation),
    typeof(ColorXAddValueHDR),

    typeof(ColorXMulHue),
    typeof(ColorXMulSaturation),
    typeof(ColorXMulValue),
  ];

  static readonly HashSet<Type> ColorBlendGroup = [
    typeof(ColorXAdditiveBlend),
    typeof(ColorXAlphaBlend),
    typeof(ColorXSoftAdditiveBlend),
    typeof(ColorXMultiplicativeBlend),
  ];

  static readonly HashSet<Type> ColorValueGroup = [

  ];

  static readonly HashSet<Type> AllColorGroups = [
    .. ColorRGBAGroup,
    .. ColorHSVGroup,
    .. ColorBlendGroup,
  ];

  internal static IEnumerable<MenuItem> ColorGroupItems(ContextualContext context)
  {
    if (AllColorGroups.Contains(context.NodeType))
    {
      foreach (var match in ColorRGBAGroup)
      {
        yield return new MenuItem(match, group: "RGBA");
      }
      foreach (var match in ColorHSVGroup)
      {
        yield return new MenuItem(match, group: "HSV");
      }
      foreach (var match in ColorBlendGroup)
      {
        yield return new MenuItem(match, group: "Blending");
      }
    }
  }
}