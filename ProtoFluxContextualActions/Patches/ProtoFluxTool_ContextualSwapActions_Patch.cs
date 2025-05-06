using System;
using Elements.Core;
using FrooxEngine;
using FrooxEngine.ProtoFlux;

using ProtoFluxContextualActions.Attributes;
using ProtoFluxContextualActions.Extensions;
using HarmonyLib;
using System.Runtime.CompilerServices;
using System.Collections.Generic;
using ProtoFlux.Runtimes.Execution.Nodes.FrooxEngine.Transform;
using ProtoFlux.Core;
using System.Linq;
using FrooxEngine.Undo;
using ProtoFlux.Runtimes.Execution.Nodes;
using ProtoFlux.Runtimes.Execution.Nodes.Math.Easing;
using ProtoFlux.Runtimes.Execution.Nodes.Operators;
using ProtoFlux.Runtimes.Execution.Nodes.Math;
using ProtoFlux.Runtimes.Execution.Nodes.TimeAndDate;
using System.Runtime.InteropServices;
using static ProtoFluxContextualActions.Extensions.DictionaryExtensions;
using System.Collections;

namespace ProtoFluxContextualActions.Patches;

[HarmonyPatch(typeof(ProtoFluxTool), nameof(ProtoFluxTool.OnSecondaryPress))]
[HarmonyPatchCategory("ProtoFluxTool Contextual Swap Actions"), TweakCategory("Adds 'Contextual Swapping Actions' to the ProtoFlux Tool. Double pressing secondary pointing at a node with protoflux tool will be open a context menu of actions to swap the node for another node.", defaultValue: true)] // unstable, disable by default
internal static class ProtoFluxTool_ContextualSwapActions_Patch
{
  // TODO: This can be replaced in the future with flags or a combination of the three automatically.
  //       progress has already been made.
  internal enum ConnectionTransferType
  {
    /// <summary>
    /// Transfers the connections by name, connections that are not found and are not of the same type will be lost.
    /// </summary>
    ByNameLossy,
    /// <summary>
    /// Uses names too :)
    /// Transfers the connections by a manually made set of mappings. Unmapped connections will be lost.
    /// </summary>
    ByMappingsLossy,
    /// <summary>
    /// Uses names too :)
    /// Attempts to match inputs of the same type 
    /// </summary>
    ByIndexLossy
  }

  internal readonly struct MenuItem(Type node, string? name = null, ConnectionTransferType? connectionTransferType = ConnectionTransferType.ByNameLossy)
  {
    internal readonly Type node = node;

    internal readonly string? name = name;

    internal readonly ConnectionTransferType? connectionTransferType = connectionTransferType;

    internal readonly string DisplayName => name ?? NodeMetadataHelper.GetMetadata(node).Name ?? node.GetNiceTypeName();
  }

  // additional data we store for the protoflux tool
  internal class ProtoFluxToolData
  {
    internal DateTime? lastSecondaryPress;
    internal ProtoFluxNode? lastSecondaryPressNode;
    internal Type? lastSpawnNodeType;

    internal double SecondsSinceLastSecondaryPress() => (DateTime.Now - lastSecondaryPress.GetValueOrDefault()).TotalSeconds;
  }

  // TODO: configurable
  const double DoublePressTime = 0.45;

  private static readonly ConditionalWeakTable<ProtoFluxTool, ProtoFluxToolData> additionalData = new();

  internal static bool Prefix(ProtoFluxTool __instance, SyncRef<ProtoFluxElementProxy> ____currentProxy)
  {
    var data = additionalData.GetOrCreateValue(__instance);
    var elementProxy = ____currentProxy.Target;

    if (elementProxy is null)
    {
      var hit = GetHit(__instance);
      if (hit is { Collider.Slot: var hitSlot })
      {
        var hitNode = hitSlot.GetComponentInParents<ProtoFluxNode>();
        if (hitNode != null)
        {
          if (data.SecondsSinceLastSecondaryPress() < DoublePressTime && data.lastSecondaryPressNode != null && !data.lastSecondaryPressNode.IsRemoved && data.lastSecondaryPressNode == hitNode)
          {
            CreateMenu(__instance, hitNode);
            data.lastSecondaryPressNode = null;
            data.lastSecondaryPressNode = null;
            data.lastSpawnNodeType = null;
            // skip rest
            return false;
          }
          else
          {
            data.lastSpawnNodeType = __instance.SpawnNodeType;
            data.lastSecondaryPressNode = hitNode;
            data.lastSecondaryPress = DateTime.Now;
            // skip null
            return true;
          }
        }
      }

      data.lastSecondaryPressNode = null;
      data.lastSecondaryPress = null;
      data.lastSpawnNodeType = null;
    }

    return true;
  }

  internal static void TransferOutputs(INode from, INode to, NodeGroup group, bool tryByIndex = false)
  {
    var query = new NodeQueryAcceleration(group);

    // resize dynamic inputs to fit before transferring the outputs
    foreach (var fromOutputListMeta in from.Metadata.DynamicOutputs)
    {
      if (to.Metadata.GetOutputListByName(fromOutputListMeta.Name) is OutputListMetadata toOutputListMeta && fromOutputListMeta.TypeConstraint == toOutputListMeta.TypeConstraint)
      {
        var toOutputList = to.GetOutputList(toOutputListMeta.Index);
        var fromOutputList = from.GetOutputList(fromOutputListMeta.Index);

        if (toOutputList.Count < fromOutputList.Count)
        {
          for (int i = 0; i < fromOutputList.Count - toOutputList.Count; i++)
          {
            toOutputList.AddOutput();
          }
        }
      }
    }

    if (tryByIndex || true)
    {
      foreach (var node in query.GetEvaluatingNodes(from))
      {
        for (int i = 0; i < node.InputCount; i++)
        {
          var fromSource = node.GetInputSource(i);
          if (fromSource?.OwnerNode == from)
          {
            var toSourceIndex = fromSource.FindLinearOutputIndex();
            node.SetInputSource(i, to.GetOutput(toSourceIndex));
          }
        }
      }
    }
  }


  internal static void TransferInputs(INode from, INode to, bool tryByIndex = false)
  {
    // resize dynamic inputs to fit before transferring the inputs
    foreach (var fromInputListMeta in from.Metadata.DynamicInputs)
    {
      if (to.Metadata.GetInputListByName(fromInputListMeta.Name) is InputListMetadata toInputListMeta && fromInputListMeta.TypeConstraint == toInputListMeta.TypeConstraint)
      {
        var toInputList = to.GetInputList(toInputListMeta.Index);
        var fromInputList = from.GetInputList(fromInputListMeta.Index);

        if (toInputList.Count < fromInputList.Count)
        {
          for (int i = 0; i < fromInputList.Count - toInputList.Count; i++)
          {
            toInputList.AddInput(null);
          }
        }
      }
    }

    if (tryByIndex)
    {
      for (int i = 0; i < MathX.Min(from.InputCount, to.InputCount); i++)
      {
        if (from.GetInputType(i) == to.GetInputType(i) && from.GetInputSource(i) is IOutput output)
        {
          to.SetInputSource(i, output);
        }
      }
    }

    foreach (var fromInputMeta in from.Metadata.FixedInputs)
    {
      if (to.Metadata.GetInputByName(fromInputMeta.Name) is InputMetadata toInputMeta)
      {
        if (fromInputMeta.InputType != toInputMeta.InputType) continue;
        if (from.GetInputSource(fromInputMeta.Index) is IOutput output)
        {
          to.SetInputSource(new ElementRef(toInputMeta.Index), output);
        }
      }
    }

    foreach (var fromInputListMeta in from.Metadata.DynamicInputs)
    {
      if (to.Metadata.GetInputListByName(fromInputListMeta.Name) is InputListMetadata toInputListMeta)
      {
        if (fromInputListMeta.TypeConstraint != toInputListMeta.TypeConstraint) continue;

        var toInputList = to.GetInputList(toInputListMeta.Index);
        var fromInputList = from.GetInputList(fromInputListMeta.Index);
        for (int i = 0; i < fromInputList.Count; i++)
        {
          if (fromInputList.GetInputSource(i) is IOutput output)
          {
            toInputList.SetInputSource(i, output);
          }
        }
        fromInputList.Clear();
      }
    }

    // This can be made into a lookup or something nicer later if it comes up again, this is fine for now.
    var typeTuple = (from.GetType(), to.GetType());
    if (typeTuple == (typeof(For), typeof(RangeLoopInt)))
    {
      var countIndex = from.Metadata.GetInputByName("Count").Index;
      var endIndex = to.Metadata.GetInputByName("End").Index;
      if (from.GetInputSource(countIndex) is IOutput output)
      {
        to.SetInputSource(endIndex, output);
      }
    }
    if (typeTuple == (typeof(RangeLoopInt), typeof(For)))
    {
      var endIndex = from.Metadata.GetInputByName("End").Index;
      var countIndex = to.Metadata.GetInputByName("Count").Index;
      if (from.GetInputSource(endIndex) is IOutput output)
      {
        to.SetInputSource(countIndex, output);
      }
    }
  }

  private static void CreateMenu(ProtoFluxTool __instance, ProtoFluxNode hitNode)
  {
    __instance.StartTask(async () =>
    {
      var items = GetMenuItems(__instance, hitNode).Where(m => m.node != hitNode.NodeType).Take(10).ToArray();

      if (items.Length > 0)
      {
        // restore previous spawn node
        __instance.SpawnNodeType.Value = additionalData.GetOrCreateValue(__instance).lastSpawnNodeType;

        var menu = await __instance.LocalUser.OpenContextMenu(__instance, __instance.Slot);
        // TODO: pages / custom menus

        foreach (var menuItem in items)
        {
          AddMenuItem(__instance, menu, colorX.White, menuItem, () =>
          {
            try
            {
              SwapHitForNode(__instance, hitNode, menuItem);
            }
            finally
            {
              // if there's somehow an error I do not want evil dangling references that world crash silently.
              if (hitNode != null && !hitNode.IsRemoved)
              {
                hitNode.UndoableDestroy();
              }
            }
          });
        }
      }
    });
  }

  private static void SwapHitForNode(ProtoFluxTool __instance, ProtoFluxNode hitNode, MenuItem menuItem)
  {
    var undoBatch = __instance.World.BeginUndoBatch($"Swap {hitNode.Name} to {menuItem.DisplayName}");

    var runtime = hitNode.NodeInstance.Runtime;
    var oldNode = hitNode.NodeInstance;
    var binding = ProtoFluxHelper.GetBindingForNode(menuItem.node);
    var overload = new NodeOverloadContext(oldNode.Runtime.Group, oldNode.Runtime);
    // overload.TrySwap(oldNode, menuItem.node);
    // overload.SwapNodes();

    void SwapNodes()
    {
      var swappedNodes = new Dictionary<INode, INode>();
      var query = new NodeQueryAcceleration(oldNode.Runtime.Group);
      var newNode = runtime.AddNode(menuItem.node);
      swappedNodes.Add(oldNode, newNode);

      // our own swapping behaviors
      {
        // ensure input list
        if (newNode.DynamicInputCount > 0 && newNode.ArgumentCount < oldNode.ArgumentCount)
        {
          var list = newNode.GetInputList(0);
          while (newNode.ArgumentCount < oldNode.ArgumentCount) list.AddInput(null);
        }

        // ensure output list
        if (newNode.DynamicOutputCount > 0 && newNode.OutputCount < oldNode.OutputCount)
        {
          var list = newNode.GetOutputList(0);
          while (newNode.OutputCount < oldNode.OutputCount) list.AddOutput();
        }
        // todo: Impulses, Operations, References, Globals

        // while SwapNodes should handle things for us, it does not handle everything so we use our own as well;
        runtime.TranslateInputs(newNode, oldNode, swappedNodes, []);
        TransferInputs(oldNode, newNode, tryByIndex: menuItem.connectionTransferType == ConnectionTransferType.ByIndexLossy);
        // by now oldNode has lost the group while newNode has inherited it
        TransferOutputs(oldNode, newNode, newNode.Runtime.Group, tryByIndex: menuItem.connectionTransferType == ConnectionTransferType.ByIndexLossy);
      }

      // newNode.CopyDynamicOutputLayout(oldNode);
      newNode.CopyDynamicOperationLayout(oldNode);
      // runtime.TranslateInputs(newNode, oldNode, swappedNodes, []);
      runtime.TranslateImpulses(newNode, oldNode, swappedNodes);
      runtime.TranslateReferences(newNode, oldNode, swappedNodes);

      var evaluatingNodes = query.GetEvaluatingNodes(oldNode).Where(n => n != oldNode);
      // foreach (var evaluatingNode in evaluatingNodes)
      // {
      //   for (int i = 0; i < evaluatingNode.InputCount; i++)
      //   {
      //     IOutput inputSource = evaluatingNode.GetInputSource(i);
      //     IOutput output = inputSource.RemapOutput(swappedNodes);
      //     if (output != inputSource)
      //     {
      //       evaluatingNode.SetInputSource(i, output);
      //     }
      //   }
      // }

      var impulsingNodes = query.GetImpulsingNodes(oldNode).Where(n => n != oldNode);
      foreach (var impulsingNode in impulsingNodes)
      {
        for (int i = 0; i < impulsingNode.ImpulseCount; i++)
        {
          var impulseTarget = impulsingNode.GetImpulseTarget(i);
          if (impulseTarget != null)
          {
            var operation = impulseTarget.RemapTarget(swappedNodes);
            if (operation != impulseTarget)
            {
              impulsingNode.SetImpulseTarget(i, operation);
            }
          }
        }
      }

      runtime.RemapImportsAndExports(swappedNodes);
      runtime.RemoveNode(oldNode);

      var t = Traverse.Create(overload);
      t.Field<Dictionary<INode, INode>>("swappedNodes").Value = swappedNodes;
      t.Field<HashSet<INode>>("affectedEvaluatingNodes").Value = [.. evaluatingNodes];
      t.Field<HashSet<INode>>("affectedImpulsingNodes").Value = [.. impulsingNodes];
    }

    SwapNodes();

    var result = ConnectionResult.Overload(overload);

    MapCastsAndOverloads(hitNode.Group, hitNode, hitNode, result, undoable: true);

    __instance.World.EndUndoBatch();
  }

  private static void AddMenuItem(ProtoFluxTool __instance, ContextMenu menu, colorX color, MenuItem item, Action setup)
  {
    var nodeMetadata = NodeMetadataHelper.GetMetadata(item.node);
    var label = (LocaleString)item.DisplayName;
    var menuItem = menu.AddItem(in label, (Uri?)null, color);
    menuItem.Button.LocalPressed += (button, data) =>
    {
      setup();
      __instance.LocalUser.CloseContextMenu(__instance);
    };
  }

  static readonly HashSet<Type> GetDirectionGroup = [
    typeof(GetForward),
    typeof(GetBackward),
    typeof(GetUp),
    typeof(GetDown),
    typeof(GetLeft),
    typeof(GetRight)
  ];

  // todo: async
  static readonly HashSet<Type> ForLoopGroup = [
    typeof(For),
    typeof(RangeLoopInt),
  ];

  // todo: currently there's too many, page support or custom uix menus are needed
  static readonly HashSet<Type> EasingGroupFloat = [
    typeof(EaseInBounceFloat),
    typeof(EaseInCircularFloat),
    typeof(EaseInCubicFloat),
    typeof(EaseInElasticFloat),
    typeof(EaseInExponentialFloat),
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
    typeof(EaseInQuadraticFloat),
    typeof(EaseInQuarticFloat),
    typeof(EaseInQuinticFloat),
    typeof(EaseInReboundFloat),
    typeof(EaseInSineFloat),
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

  static readonly HashSet<Type> EasingGroupDouble = [
    typeof(EaseInBounceDouble),
    typeof(EaseInCircularDouble),
    typeof(EaseInCubicDouble),
    typeof(EaseInElasticDouble),
    typeof(EaseInExponentialDouble),
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
    typeof(EaseInQuadraticDouble),
    typeof(EaseInQuarticDouble),
    typeof(EaseInQuinticDouble),
    typeof(EaseInReboundDouble),
    typeof(EaseInSineDouble),
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

  static readonly BiDictionary<Type, Type> MultiInputMappingGroup = new() {
    {typeof(ValueAdd<>), typeof(ValueAddMulti<>)},
    {typeof(ValueSub<>), typeof(ValueSubMulti<>)},
    {typeof(ValueMul<>), typeof(ValueMulMulti<>)},
    {typeof(ValueDiv<>), typeof(ValueDivMulti<>)},
    {typeof(ValueMin<>), typeof(ValueMinMulti<>)},
    {typeof(ValueMax<>), typeof(ValueMaxMulti<>)},
  };


  static readonly HashSet<Type> TimespanInstanceGroup = [
    typeof(TimeSpanFromDays),
    typeof(TimeSpanFromHours),
    typeof(TimeSpanFromMilliseconds),
    typeof(TimeSpanFromMinutes),
    typeof(TimeSpanFromSeconds),
    typeof(TimeSpanFromTicks),
  ];

  static readonly HashSet<Type> ArithmeticBinaryOperatorGroup = [
    typeof(ValueAdd<>),
    typeof(ValueSub<>),
    typeof(ValueMul<>),
    typeof(ValueDiv<>),
    typeof(ValueMod<>),
  ];

  static readonly HashSet<Type> ArithmeticMultiOperatorGroup = [
    typeof(ValueAddMulti<>),
    typeof(ValueSubMulti<>),
    typeof(ValueMulMulti<>),
    typeof(ValueDivMulti<>),
  ];


  static readonly HashSet<Type> ComparisonBinaryOperatorGroup = [
    typeof(ValueLessThan<>),
    typeof(ValueLessOrEqual<>),
    typeof(ValueGreaterThan<>),
    typeof(ValueGreaterOrEqual<>),
    typeof(ValueEquals<>),
    typeof(ValueNotEquals<>),
  ];

  // static readonly HashSet<Type> BooleanOperatorsGroup = [
  //   typeof(AND_Bool),
  //   typeof(NAND_Bool),
  //   typeof(NOR_Bool),
  //   typeof(OR_Bool),
  //   typeof(XNOR_Bool),
  //   typeof(XOR_Bool),
  // ];

  static readonly HashSet<Type> ValueRelayGroup = [
    typeof(ValueRelay<>),
    typeof(ContinuouslyChangingValueRelay<>)
  ];

  static readonly HashSet<Type> ObjectRelayGroup = [
    typeof(ObjectRelay<>),
    typeof(ContinuouslyChangingObjectRelay<>)
  ];

  static readonly Dictionary<Type, Type> protoFluxBindingMapping =
    Traverse.Create(typeof(ProtoFluxHelper)).Field<Dictionary<Type, Type>>("protoFluxToBindingMapping").Value.ToDictionary(a => a.Value, a => a.Key);

  internal static IEnumerable<MenuItem> GetMenuItems(ProtoFluxTool __instance, ProtoFluxNode nodeComponent)
  {
    var node = nodeComponent.NodeInstance;
    var nodeType = node.GetType();
    var componentType = nodeComponent.GetType();

    if (GetDirectionGroup.Contains(nodeType))
    {
      foreach (var match in GetDirectionGroup)
      {
        yield return new MenuItem(match);
      }
    }

    if (ForLoopGroup.Contains(nodeType))
    {
      foreach (var match in ForLoopGroup)
      {
        yield return new MenuItem(match, connectionTransferType: ConnectionTransferType.ByMappingsLossy);
      }
    }

    if (EasingGroupFloat.Contains(nodeType))
    {
      foreach (var match in EasingGroupFloat)
      {
        yield return new MenuItem(match);
      }
    }

    if (EasingGroupDouble.Contains(nodeType))
    {
      foreach (var match in EasingGroupDouble)
      {
        yield return new MenuItem(match);
      }
    }

    if (TimespanInstanceGroup.Contains(nodeType))
    {
      foreach (var match in TimespanInstanceGroup)
      {
        yield return new MenuItem(match);
      }
    }

    {
      // todo: cache per-world?
      // realistically with current resonite it doesn't matter and only needs to be done once.
      var binaryOperations =
        MapPsuedoGenericsToGenericTypes(__instance.World, "AND_")
        .Concat(MapPsuedoGenericsToGenericTypes(__instance.World, "OR_"))
        .Concat(MapPsuedoGenericsToGenericTypes(__instance.World, "NAND_"))
        .Concat(MapPsuedoGenericsToGenericTypes(__instance.World, "NOR_"))
        .Concat(MapPsuedoGenericsToGenericTypes(__instance.World, "XNOR_"))
        .Concat(MapPsuedoGenericsToGenericTypes(__instance.World, "XOR_"))
        .ToDictionary((a) => protoFluxBindingMapping[a.Node], (a) => a.Types);

      if (binaryOperations.TryGetValue(nodeType, out var genericTypes))
      {
        var matchingNodes = binaryOperations.Where(a => genericTypes.SequenceEqual(a.Value)).Select(a => a.Key);
        foreach (var match in matchingNodes)
        {
          yield return new MenuItem(match);
        }
      }
    }

    if (nodeType.TryGetGenericTypeDefinition(out var genericType))
    {
      if (ValueRelayGroup.Contains(genericType))
      {
        foreach (var match in ValueRelayGroup)
        {
          yield return new MenuItem(match.MakeGenericType(nodeType.GenericTypeArguments[0]));
        }
      }

      if (ObjectRelayGroup.Contains(genericType))
      {
        foreach (var match in ObjectRelayGroup)
        {
          yield return new MenuItem(match.MakeGenericType(nodeType.GenericTypeArguments[0]));
        }
      }

      if (ArithmeticBinaryOperatorGroup.Contains(genericType))
      {
        var opType = nodeType.GenericTypeArguments[0];
        var coder = Traverse.Create(typeof(Coder<>).MakeGenericType(opType));

        if (coder.Property<bool>("SupportsAddSub").Value)
        {
          yield return new MenuItem(typeof(ValueAdd<>).MakeGenericType(opType));
          yield return new MenuItem(typeof(ValueSub<>).MakeGenericType(opType));
        }

        if (coder.Property<bool>("SupportsMul").Value)
        {
          yield return new MenuItem(typeof(ValueMul<>).MakeGenericType(opType));
        }

        if (coder.Property<bool>("SupportsDiv").Value)
        {
          yield return new MenuItem(typeof(ValueDiv<>).MakeGenericType(opType));
        }

        if (coder.Property<bool>("SupportsMod").Value)
        {
          yield return new MenuItem(typeof(ValueMod<>).MakeGenericType(opType));
        }
      }

      if (ArithmeticMultiOperatorGroup.Contains(genericType))
      {
        var opType = nodeType.GenericTypeArguments[0];
        var coder = Traverse.Create(typeof(Coder<>).MakeGenericType(opType));

        static MenuItem MultiMenuItem(Type nodeType) => new(
          node: nodeType,
          name: nodeType.GetNiceTypeName(),
          connectionTransferType: ConnectionTransferType.ByIndexLossy
        );

        if (coder.Property<bool>("SupportsAddSub").Value)
        {
          yield return MultiMenuItem(typeof(ValueAddMulti<>).MakeGenericType(opType));
          yield return MultiMenuItem(typeof(ValueSubMulti<>).MakeGenericType(opType));
        }

        if (coder.Property<bool>("SupportsMul").Value)
        {
          yield return MultiMenuItem(typeof(ValueMulMulti<>).MakeGenericType(opType));
        }

        if (coder.Property<bool>("SupportsDiv").Value)
        {
          yield return MultiMenuItem(typeof(ValueDivMulti<>).MakeGenericType(opType));
        }
      }

      if (ComparisonBinaryOperatorGroup.Contains(genericType))
      {
        foreach (var match in ComparisonBinaryOperatorGroup)
        {
          yield return new MenuItem(match.MakeGenericType(nodeType.GenericTypeArguments[0]));
        }
      }

      if (MultiInputMappingGroup.TryGetSecond(genericType, out var mapped))
      {
        var binopType = nodeType.GenericTypeArguments[0];
        yield return new MenuItem(
          node: mapped.MakeGenericType(binopType),
          name: mapped.GetNiceTypeName(),
          connectionTransferType: ConnectionTransferType.ByIndexLossy
        );
      }
      else if (MultiInputMappingGroup.TryGetFirst(genericType, out mapped))
      {
        var binopType = nodeType.GenericTypeArguments[0];
        yield return new MenuItem(mapped.MakeGenericType(binopType), connectionTransferType: ConnectionTransferType.ByIndexLossy);
      }
    }
  }

  // Utils
  static bool TryGetGenericTypeDefinition(this Type type, out Type? genericTypeDefinition)
  {
    if (type.IsGenericType)
    {
      genericTypeDefinition = type.GetGenericTypeDefinition();
      return true;
    }
    genericTypeDefinition = null;
    return false;
  }

  public static IEnumerable<(Type Node, IEnumerable<Type> Types)> MapPsuedoGenericsToGenericTypes(World world, string startingWith)
  {
    var protoFluxNodes = Traverse.Create(typeof(ProtoFluxHelper)).Field<Dictionary<string, Type>>("protoFluxNodes").Value;
    return protoFluxNodes.Values
      .Select(t => (name: t.GetNiceTypeName(), type: t))
      .Where(a => a.name.StartsWith(startingWith) && !a.type.IsGenericType)
      .Select(a => (a.type, ParseUnderscoreGenerics(world, a.name.Substring(startingWith.Length))))
      // skip non matching
      .Where(a => a.Item2.All(t => t != null));
  }

  class ArrayComparer<T> : EqualityComparer<T[]>
  {
    public override bool Equals(T[] x, T[] y) =>
      StructuralComparisons.StructuralEqualityComparer.Equals(x, y);

    public override int GetHashCode(T[] obj) =>
      StructuralComparisons.StructuralEqualityComparer.GetHashCode(obj);
  }

  static IEnumerable<Type> ParseUnderscoreGenerics(World world, string generics) =>
    generics.Split('_').Select(name => world.Types.DecodeType(name.ToLower()) ?? world.Types.DecodeType(name));


  [HarmonyReversePatch]
  [HarmonyPatch(typeof(ProtoFluxTool), "CleanupDraggedWire")]
  [MethodImpl(MethodImplOptions.NoInlining)]
  internal static void CleanupDraggedWire(ProtoFluxTool instance) => throw new NotImplementedException();

  [HarmonyReversePatch]
  [HarmonyPatch(typeof(ProtoFluxTool), "OnSecondaryPress")]
  [MethodImpl(MethodImplOptions.NoInlining)]
  internal static void OnSecondaryPress(ProtoFluxTool instance) => throw new NotImplementedException();


  [HarmonyReversePatch]
  [HarmonyPatch(typeof(ProtoFluxHelper), "GetNodeForType")]
  [MethodImpl(MethodImplOptions.NoInlining)]
  internal static Type GetNodeForType(Type type, List<NodeTypeRecord> list) => throw new NotImplementedException();

  [HarmonyReversePatch]
  [HarmonyPatch(typeof(Tool), "GetHit")]
  [MethodImpl(MethodImplOptions.NoInlining)]
  internal static RaycastHit? GetHit(Tool instance) => throw new NotImplementedException();

  [HarmonyReversePatch]
  [HarmonyPatch(typeof(ProtoFluxNodeGroup), "MapCastsAndOverloads")]
  [MethodImpl(MethodImplOptions.NoInlining)]
  internal static void MapCastsAndOverloads(ProtoFluxNodeGroup instance, ProtoFluxNode sourceNode, ProtoFluxNode targetNode, ConnectionResult result, bool undoable) => throw new NotImplementedException();

  [HarmonyReversePatch]
  [HarmonyPatch(typeof(ProtoFluxNode), "AssociateInstance")]
  [MethodImpl(MethodImplOptions.NoInlining)]
  internal static void AssociateInstance(ProtoFluxNode instance, ProtoFluxNodeGroup group, INode node) => throw new NotImplementedException();

  // [HarmonyReversePatch]
  // [HarmonyPatch(typeof(ProtoFluxNode), "ReverseMapElements")]
  // [MethodImpl(MethodImplOptions.NoInlining)]
  // internal static void ReverseMapElements(ProtoFluxNode instance, Dictionary<INode, ProtoFluxNode> nodeMapping, bool undoable) => throw new NotImplementedException();
}

// todo: optimization pass with static dictionary
// var nodesWithGenericArguments = new Dictionary<Type[], List<Type>>(binaryOperations.Count, new ArrayComparer<Type>());
// foreach (var (psuedoGenericNode, types) in binaryOperations) nodesWithGenericArguments.Add(types, psuedoGenericNode);

// if (nodesWithGenericArguments.TryGetValue(genericTypes, out var matchingNodes))
// {
//   foreach (var match in matchingNodes)
//   {
//     yield return new MenuItem(match);
//   }
// }