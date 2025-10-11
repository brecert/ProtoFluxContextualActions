using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using FrooxEngine;
using ProtoFluxContextualActions.Patches;

namespace ProtoFluxContextualActions.Utils;

using PsuedoGenerics = IEnumerable<(Type Node, IEnumerable<Type> Types)>;

class PsuedoGenericTypes(World world)
{
  internal World World = world;

  // We will convert the types to protoflux nodes rather than frooxengine ones for convenience 
  // this is technically not "correct" behavior, but it saves us trouble right now
  internal PsuedoGenerics MapTypes(string startingWith) =>
    PsuedoGenericUtils.MapPsuedoGenericsToGenericTypes(World, startingWith)
      .Select(a => (NodeUtils.ProtoFluxBindingMapping[a.Node], a.Types));

  public PsuedoGenerics Acos { get => field ??= MapTypes("Acos_"); }
  public PsuedoGenerics Add { get => field ??= MapTypes("Add_"); }
  public PsuedoGenerics Angle { get => field ??= MapTypes("Angle_"); }
  public PsuedoGenerics AngularVelocityDelta { get => field ??= MapTypes("AngularVelocityDelta_"); }
  public PsuedoGenerics Approximately { get => field ??= MapTypes("Approximately_"); }
  public PsuedoGenerics ApproximatelyNot { get => field ??= MapTypes("ApproximatelyNot_"); }
  public PsuedoGenerics Asin { get => field ??= MapTypes("Asin_"); }
  public PsuedoGenerics Atan { get => field ??= MapTypes("Atan_"); }
  public PsuedoGenerics Atan2 { get => field ??= MapTypes("Atan2_"); }
  public PsuedoGenerics Avg { get => field ??= MapTypes("Avg_"); }
  public PsuedoGenerics AvgMulti { get => field ??= MapTypes("AvgMulti_"); }
  public PsuedoGenerics BezierCurve { get => field ??= MapTypes("BezierCurve_"); }
  public PsuedoGenerics Ceil { get => field ??= MapTypes("Ceil_"); }
  public PsuedoGenerics CeilToInt { get => field ??= MapTypes("CeilToInt_"); }
  public PsuedoGenerics Clamp01 { get => field ??= MapTypes("Clamp01_"); }
  public PsuedoGenerics Compose_Rotation { get => field ??= MapTypes("Compose_Rotation_"); }
  public PsuedoGenerics Compose_ScaleRotation { get => field ??= MapTypes("Compose_ScaleRotation_"); }
  public PsuedoGenerics ComposeTRS { get => field ??= MapTypes("ComposeTRS_"); }

  public PsuedoGenerics Log { get => field ??= MapTypes("Log_"); }
  public PsuedoGenerics Log10 { get => field ??= MapTypes("Log10_"); }
  public PsuedoGenerics LogN { get => field ??= MapTypes("LogN_"); }


  public PsuedoGenerics AND { get => field ??= MapTypes("AND_"); }
  public PsuedoGenerics OR { get => field ??= MapTypes("OR_"); }
  public PsuedoGenerics NAND { get => field ??= MapTypes("NAND_"); }
  public PsuedoGenerics NOR { get => field ??= MapTypes("NOR_"); }
  public PsuedoGenerics XNOR { get => field ??= MapTypes("XNOR_"); }
  public PsuedoGenerics XOR { get => field ??= MapTypes("XOR_"); }

  public PsuedoGenerics AND_Multi { get => field ??= MapTypes("AND_Multi_"); }
  public PsuedoGenerics OR_Multi { get => field ??= MapTypes("OR_Multi_"); }
  public PsuedoGenerics NAND_Multi { get => field ??= MapTypes("NAND_Multi_"); }
  public PsuedoGenerics NOR_Multi { get => field ??= MapTypes("NOR_Multi_"); }
  public PsuedoGenerics XNOR_Multi { get => field ??= MapTypes("XNOR_Multi_"); }
  public PsuedoGenerics XOR_Multi { get => field ??= MapTypes("XOR_Multi_"); }

  public PsuedoGenerics Sin { get => field ??= MapTypes("Sin_"); }
  public PsuedoGenerics Cos { get => field ??= MapTypes("Cos_"); }
}