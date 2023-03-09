using BaseX;
using FrooxEngine;
using FrooxEngine.LogiX.ProgramFlow;
using HarmonyLib;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CustomEntityFramework.Functions
{
    [HarmonyPatch(typeof(DynamicImpulseTrigger))]
    internal static class DynamicImpulseTriggerPatch
    {
        [HarmonyPrefix]
        [HarmonyPatch(nameof(DynamicImpulseTrigger.Run))]
        private static bool RunPrefix(DynamicImpulseTrigger __instance)
        {
            var tag = __instance.Tag.Evaluate();
            var slot = __instance.TargetHierarchy.Evaluate();
            var excludeDisabled = __instance.ExcludeDisabled.Evaluate(def: false);

            if (slot == null)
                return false;

            if (tag.StartsWith(CustomFunctionLibrary.DynamicImpulseTagPrefix))
            {
                var name = tag.Remove(0, CustomFunctionLibrary.DynamicImpulseTagPrefix.Length);
                CustomFunctionLibrary.InvokeAction(name);
            }

            var receivers = Pool.BorrowList<DynamicImpulseReceiver>();
            slot.GetComponentsInChildren(receivers, (DynamicImpulseReceiver r) => r.Tag.Evaluate() == tag, excludeDisabled);

            foreach (var receiver in receivers)
                receiver.Impulse.Trigger();

            Pool.Return(ref receivers);
            __instance.OnTriggered.Trigger();

            return false;
        }
    }
}