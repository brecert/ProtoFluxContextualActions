using System;
using System.Collections.Generic;
using System.Linq;
using Elements.Core;
using ProtoFlux.Runtimes.Execution.Nodes.Color;
using ProtoFlux.Runtimes.Execution.Nodes.FrooxEngine.Slots;
using ProtoFlux.Runtimes.Execution.Nodes.Operators;

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

  static readonly HashSet<Type> ColorPackGroup = [
    typeof(Pack_ColorX),
    typeof(HSL_ToColorX),
    typeof(HSV_ToColorX),
    typeof(ColorXHue),
    typeof(ColorXFromHexCode),
  ];

  static readonly HashSet<Type> ColorUnpackGroup = [
    typeof(Unpack_ColorX),
    typeof(ColorXToHSV),
    typeof(ColorXToHSL),
    typeof(ColorXToHexCode),
  ];

  static readonly HashSet<Type> AllColorGroups = [
    .. ColorRGBAGroup,
    .. ColorHSVGroup,
    .. ColorBlendGroup,
    .. ColorPackGroup,
    .. ColorUnpackGroup,
  ];

  internal static IEnumerable<MenuItem> ColorGroupItems(ContextualContext context)
  {
    if (AllColorGroups.Contains(context.NodeType))
    {
      string[] rgbaNames = ["Red", "Green", "Blue", "Alpha"];
      string[] hsvNames = ["Hue", "Saturation", "Value"];
      foreach (var match in ColorRGBAGroup)
      {
        string name = rgbaNames.First(v => match.Name.Contains(v));
        yield return new MenuItem(match, group: "RGBA/" + name);
      }
      foreach (var match in ColorHSVGroup)
      {
        string name = hsvNames.First(v => match.Name.Contains(v));
        yield return new MenuItem(match, group: "HSV/" + name);
      }
      foreach (var match in ColorBlendGroup)
      {
        yield return new MenuItem(match, group: "Blending");
      }
      foreach (var match in ColorPackGroup)
      {
        yield return new MenuItem(match, group: "Packing");
      }
      foreach (var match in ColorUnpackGroup)
      {
        yield return new MenuItem(match, group: "Unpacking");
      }
    }
  }
}