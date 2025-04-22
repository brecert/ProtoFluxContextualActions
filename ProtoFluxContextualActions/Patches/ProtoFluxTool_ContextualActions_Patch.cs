using System;
using Elements.Core;
using FrooxEngine;
using FrooxEngine.ProtoFlux;

using ProtoFluxContextualActions.Attributes;
using HarmonyLib;
using System.Runtime.CompilerServices;
using System.Collections.Generic;
using System.Linq;
using ProtoFlux.Core;
using ProtoFlux.Runtimes.Execution.Nodes;
using ProtoFlux.Runtimes.Execution.Nodes.Operators;
using ProtoFlux.Runtimes.Execution.Nodes.Math.Quaternions;
using ProtoFlux.Runtimes.Execution.Nodes.FrooxEngine.Transform;
using ProtoFlux.Runtimes.Execution.Nodes.FrooxEngine.Slots;
using ProtoFlux.Runtimes.Execution.Nodes.FrooxEngine.Users;
using ProtoFlux.Runtimes.Execution.Nodes.FrooxEngine.Async;
using ProtoFlux.Runtimes.Execution.Nodes.FrooxEngine.Audio;
using SharpPipe;
using ProtoFlux.Runtimes.Execution.Nodes.TimeAndDate;
using ProtoFlux.Runtimes.Execution.Nodes.Math;
using ProtoFlux.Runtimes.Execution.Nodes.Strings.Characters;
using ProtoFlux.Runtimes.Execution.Nodes.Strings;
using ProtoFlux.Runtimes.Execution.Nodes.ParsingFormatting;

namespace ProtoFluxContextualActions.Patches;

[HarmonyPatchCategory("ProtoFluxTool Contextual Actions"), TweakCategory("Adds 'Contextual Actions' to the ProtoFlux Tool. The secondary press while holding a protoflux tool will be open a context menu of quick actions based on what wire you're dragging instead of always spawning an input node. Pressing secondary again will spawn out an input node like normal.")]
[HarmonyPatch(typeof(ProtoFluxTool), nameof(ProtoFluxTool.OnSecondaryPress))]
internal static class ProtoFluxTool_ContextualActions_Patch
{
    internal readonly struct MenuItem(Type node, Type? binding = null, string? name = null, bool overload = false)
    {
        internal readonly Type node = node;

        internal readonly Type? binding = binding;

        internal readonly string? name = name;

        internal readonly bool overload = overload;

        internal readonly string DisplayName => name ?? NodeMetadataHelper.GetMetadata(node).Name ?? node.GetNiceTypeName();
    }

    internal static bool Prefix(ProtoFluxTool __instance, SyncRef<ProtoFluxElementProxy> ____currentProxy)
    {
        var elementProxy = ____currentProxy.Target;
        var items = MenuItems(__instance).Take(10).ToArray();
        // todo: pages / menu

        if (items.Length != 0)
        {
            if (__instance.LocalUser.IsContextMenuOpen())
            {
                __instance.LocalUser.CloseContextMenu(__instance);
                return true;
            }

            __instance.StartTask(async () =>
            {
                var menu = await __instance.LocalUser.OpenContextMenu(__instance, __instance.Slot);
                Traverse.Create(menu).Field<float?>("_speedOverride").Value = 10;

                switch (elementProxy)
                {
                    case ProtoFluxInputProxy inputProxy:
                        {
                            foreach (var item in items)
                            {
                                AddMenuItem(__instance, menu, inputProxy.InputType.Value.GetTypeColor(), item, n =>
                                {
                                    if (item.overload)
                                    {
                                        __instance.StartTask(async () =>
                                        {
                                            // this is dumb
                                            // TODO: investigate why it's needed to avoid the one or two update disconnect issue
                                            await new Updates(1);
                                            var output = n.GetOutput(0); // TODO: specify
                                            elementProxy.Node.Target.TryConnectInput(inputProxy.NodeInput.Target, output, allowExplicitCast: false, undoable: true);
                                        });
                                    }
                                    else
                                    {
                                        var output = n.NodeOutputs.First(o => typeof(INodeOutput<>).MakeGenericType(inputProxy.InputType).IsAssignableFrom(o.GetType()));
                                        elementProxy.Node.Target.TryConnectInput(inputProxy.NodeInput.Target, output, allowExplicitCast: false, undoable: true);
                                    }
                                });
                            }
                            break;
                        }
                    case ProtoFluxOutputProxy outputProxy:
                        {
                            foreach (var item in items)
                            {
                                AddMenuItem(__instance, menu, outputProxy.OutputType.Value.GetTypeColor(), item, n =>
                                {
                                    if (item.overload) throw new Exception("Overloading with ProtoFluxOutputProxy is not supported");
                                    var input = n.NodeInputs.First(i => typeof(INodeOutput<>).MakeGenericType(outputProxy.OutputType).IsAssignableFrom(i.TargetType));
                                    n.TryConnectInput(input, outputProxy.NodeOutput.Target, allowExplicitCast: false, undoable: true);
                                });
                            }
                            break;
                        }
                    case ProtoFluxImpulseProxy impulseProxy:
                        {
                            foreach (var item in items)
                            {
                                // the colors should almost always be the same so unique colors are more important maybe?
                                AddMenuItem(__instance, menu, item.node.GetTypeColor(), item, n =>
                                {
                                    if (item.overload) throw new Exception("Overloading with ProtoFluxImpulseProxy is not supported");
                                    n.TryConnectImpulse(impulseProxy.NodeImpulse.Target, n.GetOperation(0), undoable: true);
                                });
                            }
                            break;
                        }
                    case ProtoFluxOperationProxy operationProxy:
                        {
                            foreach (var item in items)
                            {
                                AddMenuItem(__instance, menu, item.node.GetTypeColor(), item, n =>
                                {
                                    if (item.overload) throw new Exception("Overloading with ProtoFluxOperationProxy is not supported");
                                    n.TryConnectImpulse(n.GetImpulse(0), operationProxy.NodeOperation.Target, undoable: true);
                                });
                            }
                            break;
                        }
                    default:
                        throw new Exception("found items for unsupported protoflux contextual action type");
                }
            });

            return false;
        }

        return true;
    }

    private static void AddMenuItem(ProtoFluxTool __instance, ContextMenu menu, colorX color, MenuItem item, Action<ProtoFluxNode> setup)
    {
        var nodeMetadata = NodeMetadataHelper.GetMetadata(item.node);
        var label = (LocaleString)item.DisplayName;
        var menuItem = menu.AddItem(in label, (Uri?)null, color);
        menuItem.Button.LocalPressed += (button, data) =>
        {
            var nodeBinding = item.binding ?? ProtoFluxHelper.GetBindingForNode(item.node);
            __instance.SpawnNode(nodeBinding, n =>
            {
                n.EnsureElementsInDynamicLists();
                setup(n);
                __instance.LocalUser.CloseContextMenu(__instance);
                CleanupDraggedWire(__instance);
            });
        };
    }

    // note: if we can build up a graph then we can egraph reduce to make matches like this easier to spot automatically rather than needing to check each one manually
    // todo: detect add + 1 and offer to convert to inc?
    // todo: detect add + 1 or inc and write and offer to convert to increment?

    internal static IEnumerable<MenuItem> MenuItems(ProtoFluxTool __instance)
    {
        var _currentProxy = Traverse.Create(__instance).Field("_currentProxy").GetValue<SyncRef<ProtoFluxElementProxy>>();
        var target = _currentProxy?.Target;

        foreach (var item in GeneralNumericOperationMenuItems(target)) yield return item;

        if (target is ProtoFluxInputProxy inputProxy)
        {
            foreach (var item in InputMenuItems(inputProxy)) yield return item;
        }

        else if (target is ProtoFluxOutputProxy outputProxy)
        {
            foreach (var item in OutputMenuItems(outputProxy)) yield return item;
        }

        else if (target is ProtoFluxImpulseProxy impulseProxy)
        {
            // TODO: convert to while?
            yield return new MenuItem(typeof(For));
            yield return new MenuItem(typeof(If));
            yield return new MenuItem(typeof(ValueWrite<dummy>));
            yield return new MenuItem(typeof(Sequence));

            switch (impulseProxy.ImpulseType.Value)
            {
                case ImpulseType.AsyncCall:
                case ImpulseType.AsyncResumption:
                    yield return new MenuItem(typeof(AsyncFor));
                    yield return new MenuItem(typeof(AsyncSequence));
                    break;
            }
        }

        else if (target is ProtoFluxOperationProxy operationProxy)
        {
            if (operationProxy.IsAsync)
            {
                yield return new MenuItem(typeof(StartAsyncTask));
            }
        }
    }

    internal static IEnumerable<MenuItem> GeneralNumericOperationMenuItems(ProtoFluxElementProxy? target)
    {
        {
            // TODO: It's nice to have these work with any node, I think their precedence should be lower than manually specified ones and potentially hidden by default for many types that support but do not need, esp. comparison.
            //       When I'm more sure that Swapping won't world crash I think I can limit comparison to a single node and then swap to the right one as a sort of submenu?
            //       Feels a little weird though, ux is difficult. A custom uix menu could help.
            if (target is ProtoFluxOutputProxy { OutputType.Value: var outputType } && (outputType.IsUnmanaged() || typeof(ISphericalHarmonics).IsAssignableFrom(outputType)))
            {
                var coder = Traverse.Create(typeof(Coder<>).MakeGenericType(outputType));
                var isMatrix = typeof(IMatrix).IsAssignableFrom(outputType);
                // only handle values

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
                    yield return new MenuItem(typeof(ValueNegate<>).MakeGenericType(outputType));
                }

                if (coder.Property<bool>("SupportsMod").Value)
                {
                    yield return new MenuItem(typeof(ValueMod<>).MakeGenericType(outputType));
                }

                if (coder.Property<bool>("SupportsAbs").Value && !isMatrix)
                {
                    yield return new MenuItem(typeof(ValueAbs<>).MakeGenericType(outputType));
                }

                if (coder.Property<bool>("SupportsComparison").Value)
                {
                    yield return new MenuItem(typeof(ValueLessThan<>).MakeGenericType(outputType));
                    yield return new MenuItem(typeof(ValueLessOrEqual<>).MakeGenericType(outputType));
                    yield return new MenuItem(typeof(ValueGreaterThan<>).MakeGenericType(outputType));
                    yield return new MenuItem(typeof(ValueGreaterOrEqual<>).MakeGenericType(outputType));
                    yield return new MenuItem(typeof(ValueEquals<>).MakeGenericType(outputType));
                    yield return new MenuItem(typeof(ValueNotEquals<>).MakeGenericType(outputType));
                }

                if (TryGetInverseNode(outputType, out var inverseNodeType))
                {
                    yield return new MenuItem(inverseNodeType);
                }

                if (TryGetTransposeNode(outputType, out var transposeNodeType))
                {
                    yield return new MenuItem(transposeNodeType, name: "Transpose");
                }
            }
        }
    }

    /// <summary>
    /// Yields menu items when holding an output wire. 
    /// </summary>
    /// <param name="outputProxy"></param>
    /// <returns></returns>
    internal static IEnumerable<MenuItem> OutputMenuItems(ProtoFluxOutputProxy outputProxy)
    {
        if (TryGetUnpackNode(outputProxy.OutputType, out var unpackNodeTypes))
        {
            foreach (var nodeType in unpackNodeTypes)
            {
                yield return new MenuItem(nodeType);
            }
        }
        var outputType = outputProxy.OutputType.Value;

        if (outputType == typeof(Slot))
        {
            yield return new MenuItem(typeof(GlobalTransform));
            yield return new MenuItem(typeof(GetForward));
            yield return new MenuItem(typeof(GetChild));
            yield return new MenuItem(typeof(ChildrenCount));
        }

        else if (outputType == typeof(bool))
        {
            yield return new MenuItem(typeof(If));
            yield return new MenuItem(typeof(ValueConditional<int>)); // dummy type when // todo: convert to multi?
            yield return new MenuItem(typeof(AND_Bool));
            yield return new MenuItem(typeof(OR_Bool));
            yield return new MenuItem(typeof(NOT_Bool));
        }

        else if (outputType == typeof(string))
        {
            yield return new MenuItem(typeof(GetCharacter));
            yield return new MenuItem(typeof(StringLength));
            yield return new MenuItem(typeof(CountOccurrences));
            yield return new MenuItem(typeof(IndexOfString));
            yield return new MenuItem(typeof(Contains));
            yield return new MenuItem(typeof(Substring));
            yield return new MenuItem(typeof(FormatString));
        }

        else if (outputType == typeof(DateTime))
        {
            yield return new MenuItem(typeof(Sub_DateTime));
            yield return new MenuItem(typeof(Add_DateTime_TimeSpan));
        }

    }

    /// <summary>
    /// Generates menu items when holding an input wire.
    /// </summary>
    /// <param name="inputProxy"></param>
    /// <returns></returns>
    internal static IEnumerable<MenuItem> InputMenuItems(ProtoFluxInputProxy inputProxy)
    {
        var inputType = inputProxy.InputType.Value;
        var nodeType = inputProxy.Node.Target.NodeType;

        if (TryGetPackNode(inputType, out var packNodeTypes))
        {
            foreach (var packNodeType in packNodeTypes)
            {
                yield return new MenuItem(packNodeType);
            }
        }

        if (inputType == typeof(User))
        {
            yield return new MenuItem(typeof(LocalUser));
            yield return new MenuItem(typeof(HostUser));
            yield return new MenuItem(typeof(UserFromUsername));
            yield return new MenuItem(typeof(GetActiveUser));
            yield return new MenuItem(typeof(GetActiveUserSelf));
        }

        else if (inputType == typeof(UserRoot))
        {
            yield return new MenuItem(typeof(GetActiveUserRoot));
            yield return new MenuItem(typeof(LocalUserRoot));
            yield return new MenuItem(typeof(UserUserRoot));
        }

        else if (inputType == typeof(bool))
        {
            // I want to use dummy's here but it's not safe to do so.
            yield return new MenuItem(typeof(ValueLessThan<int>));
            yield return new MenuItem(typeof(ValueLessOrEqual<int>));
            yield return new MenuItem(typeof(ValueGreaterThan<int>));
            yield return new MenuItem(typeof(ValueGreaterOrEqual<int>));
            yield return new MenuItem(typeof(ValueEquals<int>));
        }

        else if (inputType == typeof(DateTime))
        {
            yield return new MenuItem(typeof(UtcNow));
            yield return new MenuItem(typeof(FromUnixMilliseconds));
        }

        else if (inputType == typeof(TimeSpan))
        {
            yield return new MenuItem(typeof(Parse_TimeSpan));
            yield return new MenuItem(typeof(TimeSpanFromTicks));
            yield return new MenuItem(typeof(TimeSpanFromMilliseconds));
            yield return new MenuItem(typeof(TimeSpanFromSeconds));
            yield return new MenuItem(typeof(TimeSpanFromMinutes));
            yield return new MenuItem(typeof(TimeSpanFromHours));
            yield return new MenuItem(typeof(TimeSpanFromDays));
        }

        else if (inputType == typeof(Slot))
        {
            yield return new MenuItem(typeof(RootSlot));
            yield return new MenuItem(typeof(LocalUserSlot));
        }

        else if (nodeType == typeof(ValueMul<floatQ>) && inputProxy.ElementName == "B")
        {
            yield return new MenuItem(typeof(GetForward), overload: true);
            // yield return new MenuItem(
            //     name: "ValueInput<float>",
            //     node: typeof(ExternalValueInput<FrooxEngineContext, float3>),
            //     binding: typeof(FrooxEngine.ProtoFlux.Runtimes.Execution.Nodes.ValueInput<float3>),
            //     overload: true
            // );
        }

        else if (nodeType == typeof(Mul_FloatQ_Float3) && inputProxy.ElementName == "B")
        {
            yield return new MenuItem(typeof(GetForward));
            yield return new MenuItem(typeof(GetBackward));
            yield return new MenuItem(typeof(GetUp));
            yield return new MenuItem(typeof(GetDown));
            yield return new MenuItem(typeof(GetLeft));
            yield return new MenuItem(typeof(GetRight));
        }
    }

    internal static readonly Dictionary<Type, Type[]> UnpackNodeMapping = new() {
        // "core" IVector types
        {typeof(bool2), [typeof(Unpack_Bool2)]},
        {typeof(bool3), [typeof(Unpack_Bool3)]},
        {typeof(bool4), [typeof(Unpack_Bool4)]},

        {typeof(int2), [typeof(Unpack_Int2)]},
        {typeof(int3), [typeof(Unpack_Int3)]},
        {typeof(int4), [typeof(Unpack_Int4)]},

        {typeof(long2), [typeof(Unpack_Long2)]},
        {typeof(long3), [typeof(Unpack_Long3)]},
        {typeof(long4), [typeof(Unpack_Long4)]},

        {typeof(uint2), [typeof(Unpack_Uint2)]},
        {typeof(uint3), [typeof(Unpack_Uint3)]},
        {typeof(uint4), [typeof(Unpack_Uint4)]},

        {typeof(ulong2), [typeof(Unpack_Ulong2)]},
        {typeof(ulong3), [typeof(Unpack_Ulong3)]},
        {typeof(ulong4), [typeof(Unpack_Ulong4)]},

        {typeof(float2), [typeof(Unpack_Float2)]},
        {typeof(float3), [typeof(Unpack_Float3)]},
        {typeof(float4), [typeof(Unpack_Float4)]},

        {typeof(double2), [typeof(Unpack_Double2)]},
        {typeof(double3), [typeof(Unpack_Double3)]},
        {typeof(double4), [typeof(Unpack_Double4)]},

        // quaternions
        {typeof(floatQ), [typeof(Unpack_FloatQ), typeof(EulerAngles_floatQ)]},
        {typeof(doubleQ), [typeof(Unpack_DoubleQ), typeof(EulerAngles_doubleQ)]},
        
        // colors
        {typeof(color), [typeof(Unpack_Color)]},
        {typeof(colorX), [typeof(Unpack_ColorX)]},

        {typeof(float2x2),  [typeof(UnpackRows_Float2x2), typeof(UnpackColumns_Float2x2)]},
        {typeof(float3x3),  [typeof(UnpackRows_Float3x3), typeof(UnpackColumns_Float3x3)]},
        {typeof(float4x4),  [typeof(UnpackRows_Float4x4), typeof(UnpackColumns_Float4x4)]},
        {typeof(double2x2),  [typeof(UnpackRows_Double2x2), typeof(UnpackColumns_Double2x2)]},
        {typeof(double3x3),  [typeof(UnpackRows_Double3x3), typeof(UnpackColumns_Double3x3)]},
        {typeof(double4x4),  [typeof(UnpackRows_Double4x4), typeof(UnpackColumns_Double4x4)]},
        
        // TODO: SH1<T>, SH2<T>, SH3<T>, SH4<T>
    };

    internal static bool TryGetUnpackNode(Type nodeType, out Type[]? value)
    {
        if (ReflectionHelper.IsNullable(nodeType) && Nullable.GetUnderlyingType(nodeType).IsUnmanaged())
        {
            try
            {
                value = [typeof(UnpackNullable<>).MakeGenericType(Nullable.GetUnderlyingType(nodeType))];
                return true;
            }
            catch
            {
                value = null;
                return false;
            }
        }
        return UnpackNodeMapping.TryGetValue(nodeType, out value);
    }

    // TODO: make a faux "generic" type getter for things like Unpack_Float3, Or_Multi_Bool2, or Mul_FloatQ_Float3 that interacts in a generic way to avoid this in the future
    internal static readonly Dictionary<Type, Type[]> PackNodeMapping = new() {
        {typeof(bool2), [typeof(Pack_Bool2)]},
        {typeof(bool3), [typeof(Pack_Bool3)]},
        {typeof(bool4), [typeof(Pack_Bool4)]},

        {typeof(int2), [typeof(Pack_Int2)]},
        {typeof(int3), [typeof(Pack_Int3)]},
        {typeof(int4), [typeof(Pack_Int4)]},

        {typeof(long2), [typeof(Pack_Long2)]},
        {typeof(long3), [typeof(Pack_Long3)]},
        {typeof(long4), [typeof(Pack_Long4)]},

        {typeof(uint2), [typeof(Pack_Uint2)]},
        {typeof(uint3), [typeof(Pack_Uint3)]},
        {typeof(uint4), [typeof(Pack_Uint4)]},

        {typeof(ulong2), [typeof(Pack_Ulong2)]},
        {typeof(ulong3), [typeof(Pack_Ulong3)]},
        {typeof(ulong4), [typeof(Pack_Ulong4)]},

        {typeof(float2), [typeof(Pack_Float2)]},
        {typeof(float3), [typeof(Pack_Float3)]},
        {typeof(float4), [typeof(Pack_Float4)]},

        {typeof(double2), [typeof(Pack_Double2)]},
        {typeof(double3), [typeof(Pack_Double3)]},
        {typeof(double4), [typeof(Pack_Double4)]},

        {typeof(color), [typeof(Pack_Color)]},
        {typeof(colorX), [typeof(Pack_ColorX)]},

        // quaternions
        {typeof(floatQ), [typeof(Pack_FloatQ), typeof(FromEuler_floatQ)]},
        {typeof(doubleQ), [typeof(Pack_DoubleQ), typeof(FromEuler_doubleQ)]},

        {typeof(float2x2),  [typeof(PackRows_Float2x2), typeof(PackColumns_Float2x2)]},
        {typeof(float3x3),  [typeof(PackRows_Float3x3), typeof(PackColumns_Float3x3)]},
        {typeof(float4x4),  [typeof(PackRows_Float4x4), typeof(PackColumns_Float4x4), typeof(ComposeTRS_Float4x4)]},
        {typeof(double2x2),  [typeof(PackRows_Double2x2), typeof(PackColumns_Double2x2)]},
        {typeof(double3x3),  [typeof(PackRows_Double3x3), typeof(PackColumns_Double3x3)]},
        {typeof(double4x4),  [typeof(PackRows_Double4x4), typeof(PackColumns_Double4x4), typeof(ComposeTRS_Double4x4)]},

        {typeof(ZitaParameters), [typeof(ConstructZitaParameters)]},
    };

    internal static bool TryGetPackNode(Type nodeType, out Type[] value)
    {
        if (ReflectionHelper.IsNullable(nodeType) && Nullable.GetUnderlyingType(nodeType).IsUnmanaged())
        {
            try
            {
                value = [typeof(PackNullable<>).MakeGenericType(Nullable.GetUnderlyingType(nodeType))];
                return true;
            }
            catch
            {
                value = null;
                return false;
            }
        }
        return PackNodeMapping.TryGetValue(nodeType, out value);
    }

    internal static readonly Dictionary<Type, Type> InverseNodeMapping = new()
    {
        {typeof(float2x2), typeof(Inverse_Float2x2)},
        {typeof(float3x3), typeof(Inverse_Float3x3)},
        {typeof(float4x4), typeof(Inverse_Float4x4)},
        {typeof(double2x2), typeof(Inverse_Double2x2)},
        {typeof(double3x3), typeof(Inverse_Double3x3)},
        {typeof(double4x4), typeof(Inverse_Double4x4)},
        // shh
        {typeof(floatQ), typeof(InverseRotation_floatQ)},
        {typeof(doubleQ), typeof(InverseRotation_doubleQ)},
    };

    internal static bool TryGetInverseNode(Type valueType, out Type value) => InverseNodeMapping.TryGetValue(valueType, out value);


    internal static readonly Dictionary<Type, Type> TransposeNodeMapping = new()
    {
        {typeof(float2x2), typeof(Transpose_Float2x2)},
        {typeof(float3x3), typeof(Transpose_Float3x3)},
        {typeof(float4x4), typeof(Transpose_Float4x4)},
        {typeof(double2x2), typeof(Transpose_Double2x2)},
        {typeof(double3x3), typeof(Transpose_Double3x3)},
        {typeof(double4x4), typeof(Transpose_Double4x4)},
    };

    internal static bool TryGetTransposeNode(Type valueType, out Type value) => TransposeNodeMapping.TryGetValue(valueType, out value);


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
}