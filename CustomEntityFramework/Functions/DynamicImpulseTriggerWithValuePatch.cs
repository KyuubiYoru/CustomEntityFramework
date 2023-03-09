using BaseX;
using CloudX.Shared;
using FrooxEngine;
using FrooxEngine.LogiX.Input;
using FrooxEngine.LogiX.ProgramFlow;
using HarmonyLib;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

namespace CustomEntityFramework.Functions
{
    internal static class DynamicImpulseTriggerWithValuePatch
    {
        private static readonly MethodInfo bareRunReceiverMethod = typeof(DynamicImpulseTriggerWithValuePatch).GetMethod(nameof(RunReceivers), AccessTools.all);
        private static readonly Dictionary<Type, MethodInfo> genericRunReceiversMethods = new();
        private static readonly MethodInfo prefix = typeof(DynamicImpulseTriggerWithValuePatch).GetMethod(nameof(Prefix), AccessTools.all);

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

            T value;
            if (!tag.StartsWith(CustomFunctionLibrary.DynamicImpulseTagPrefix))
            {
                value = __instance.Value.Evaluate();
            }
            else
            {
                var name = tag.Remove(0, CustomFunctionLibrary.DynamicImpulseTagPrefix.Length);
                value = CustomFunctionLibrary.InvokeFunction(name, __instance);
            }

            // Here to preserve order of evaluations from original
            var excludeDisabled = __instance.ExcludeDisabled.Evaluate();

            if (target == null)
                return false;

            var type = __instance.GetType().GetGenericArguments()[0];

            if (type.IsValueType)
                RunReceivers(target, tag, value, excludeDisabled);
            else
            {
                if (!genericRunReceiversMethods.TryGetValue(type, out var method))
                {
                    method = bareRunReceiverMethod.MakeGenericMethod(type);
                    genericRunReceiversMethods.Add(type, method);
                }

                method.Invoke(null, new object[] { target, tag, value, excludeDisabled });
            }

            __instance.OnTriggered.Trigger();

            return false;
        }

        private static void RunReceivers<T>(Slot target, string tag, T value, bool excludeDisabled)
        {
            var list = Pool.BorrowList<DynamicImpulseReceiverWithValue<T>>();
            target.GetComponentsInChildren(list, r => r.Tag.Evaluate() == tag, excludeDisabled);

            foreach (var receiver in list)
                receiver.Trigger(value);

            Pool.Return(ref list);
        }
    }
}