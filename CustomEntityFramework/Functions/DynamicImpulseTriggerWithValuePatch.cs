using BaseX;
using CloudX.Shared;
using FrooxEngine;
using FrooxEngine.LogiX;
using FrooxEngine.LogiX.Input;
using FrooxEngine.LogiX.ProgramFlow;
using HarmonyLib;
using NeosModLoader;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

namespace CustomEntityFramework.Functions
{
    internal static class DynamicImpulseTriggerWithValuePatch
    {
        private static readonly MethodInfo prefix = typeof(DynamicImpulseTriggerWithValuePatch).GetMethod(nameof(Prefix), AccessTools.all);

        private static readonly GenericMethodInvoker runReceiversInvoker = new(typeof(DynamicImpulseTriggerWithValuePatch).GetMethod(nameof(RunReceivers), AccessTools.all));

        public static void Patch(Harmony harmony)
        {
            foreach (var type in CustomEntityFramework.NeosPrimitiveAndEnumTypes)
            {
                var createdType = typeof(DynamicImpulseTriggerWithValue<>).MakeGenericType(type);
                var methodInfo = createdType.GetMethod("Run", AccessTools.allDeclared);

                harmony.Patch(methodInfo, new HarmonyMethod(prefix.MakeGenericMethod(type)));
            }

            //return CustomEntityFramework.GenerateGenericMethodTargets(
            //    CustomEntityFramework.NeosPrimitiveAndEnumTypes,
            //    nameof(DynamicImpulseTriggerWithValue<object>.Run),
            //    typeof(DynamicImpulseTriggerWithValue<>));
        }

        public static bool Prefix<T>(DynamicImpulseTriggerWithValue<T> __instance)
        {
            if (!__instance.Enabled)
                return false;

            var tag = __instance.Tag.Evaluate();
            var target = __instance.TargetHierarchy.Evaluate();

            T value = default;
            var invokeCustom = tag.StartsWith(CustomFunctionLibrary.DynamicImpulseTagPrefix);

            if (!invokeCustom)
            {
                value = __instance.Value.Evaluate();
            }

            // Here to preserve order of evaluations from original
            var excludeDisabled = __instance.ExcludeDisabled.Evaluate();

            if (target == null)
                return false;

            if (invokeCustom)
            {
                var name = tag.Remove(0, CustomFunctionLibrary.DynamicImpulseTagPrefix.Length);
                if (!CustomFunctionLibrary.InvokeFunction(name, __instance, out value))
                    return false;
            }

            var type = __instance.GetType().GetGenericArguments()[0];

            if (type.IsValueType)
                RunReceivers(__instance, target, tag, value, excludeDisabled);
            else
                runReceiversInvoker.Invoke(type, __instance, target, tag, value, excludeDisabled);

            return false;
        }

        private static void RunReceivers<T>(DynamicImpulseTriggerWithValue<T> trigger, Slot target, string tag, T value, bool excludeDisabled)
        {
            var list = Pool.BorrowList<DynamicImpulseReceiverWithValue<T>>();
            target.GetComponentsInChildren(list, r => r.Tag.Evaluate() == tag, excludeDisabled);

            NeosMod.Msg("Receiver method with type: " + typeof(T));

            foreach (var receiver in list)
                receiver.Trigger(value);

            Pool.Return(ref list);

            NeosMod.Msg("Ran receivers; triggering output");

            //trigger.OnTriggered.Trigger();
            Traverse.Create(trigger).Field<Impulse>("OnTriggered").Value.Trigger();

            NeosMod.Msg("Output triggered; reached the end.");
        }
    }
}