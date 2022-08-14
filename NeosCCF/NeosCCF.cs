using BaseX;
using CloudX.Shared;
using FrooxEngine;
using FrooxEngine.LogiX.Input;
using FrooxEngine.LogiX.ProgramFlow;
using HarmonyLib;
using NeosModLoader;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

namespace NeosCCF
{
    public class NeosCCF : NeosMod
    {
        public override string Name => "NeosCCF";
        public override string Author => "KyuubiYoru";
        public override string Version => "1.0.0";
        public override string Link => "https://github.com/KyuubiYoru/NeosCCF.git";


        public override void OnEngineInit()
        {
            Harmony harmony = new Harmony("net.KyuubiYoru.NeosCCF");
            //harmony.PatchAll();
            DynamicImpulseTriggerPatch.Patch(harmony);
        }

        class DynamicImpulseTriggerPatch
        {
            private static readonly MethodInfo prefix = typeof(DynamicImpulseTriggerPatch).GetMethod(nameof(Prefix), AccessTools.all);

            public static void Patch(Harmony harmony)
            {
                var traverse = Traverse.Create(typeof(GenericTypes));

                var neosPrimitiveTypes = traverse.Field<Type[]>("neosPrimitives").Value
                    .Where(type => type.Name != "String")
                    .AddItem(typeof(Rect))
                    .AddItem(typeof(dummy))
                    .AddItem(typeof(object))
                    .ToArray();

                var neosEnumTypes = AccessTools.GetTypesFromAssembly(typeof(EnumInput<>).Assembly)
                    .Concat(AccessTools.GetTypesFromAssembly(typeof(float4).Assembly))
                    .Concat(AccessTools.GetTypesFromAssembly(typeof(SessionAccessLevel).Assembly))
                    .Where(type => type.IsEnum)
                    .Where(PublicTypesFilter)
                    .Where(NoGenericTypesFilter)
                    .ToArray();

                var neosPrimitiveAndEnumTypes = neosPrimitiveTypes.Concat(neosEnumTypes).ToArray();

                foreach (var type in neosPrimitiveAndEnumTypes)
                {
                    var createdType = typeof(DynamicImpulseTriggerWithValue<>).MakeGenericType(type);
                    var methodInfo = createdType.GetMethod("Run", AccessTools.allDeclared);

                    harmony.Patch(methodInfo, new HarmonyMethod(prefix.MakeGenericMethod(type)));
                }
            }

            public static bool Prefix<T>(DynamicImpulseTriggerWithValue<T> __instance)
            {
                if (!__instance.Enabled)
                {
                    return false;
                }
                string tag = __instance.Tag.Evaluate();
                Slot slot = __instance.TargetHierarchy.Evaluate();
                T t = __instance.Value.Evaluate();
                bool flag = __instance.ExcludeDisabled.Evaluate();
                if (!tag.StartsWith("NeosCCF"))
                {
                    if (slot != null)
                    {
                        List<DynamicImpulseReceiverWithValue<T>> list = Pool.BorrowList<DynamicImpulseReceiverWithValue<T>>();
                        slot.GetComponentsInChildren(list, r => r.Tag.Evaluate() == tag, flag);
                        foreach (DynamicImpulseReceiverWithValue<T> dynamicImpulseReceiverWithValue in list)
                        {
                            dynamicImpulseReceiverWithValue.Trigger(t);
                        }
                        Pool.Return(ref list);
                    }
                }
                else
                {
                    //Run modded code

                    try
                    {
                        var value = __instance.Value.Evaluate();
                        string result = value.ToString() + " type: " + value.GetType();
                        SendImpulseString(flag, slot, "echo", result);
                    }
                    catch (Exception e)
                    {
                        Debug(e.Message);
                        Debug(e);
                    }
                }

                __instance.OnTriggered.Trigger();
                return false;
            }
        }

        private static void SendImpulseString(bool flag, Slot slot, string tag, string value)
        {
            if (slot != null)
            {
                List<DynamicImpulseReceiverWithValue<string>> list = Pool.BorrowList<DynamicImpulseReceiverWithValue<string>>();
                slot.GetComponentsInChildren(list, r => r.Tag.Evaluate(null) == tag, flag);
                foreach (DynamicImpulseReceiverWithValue<string> dynamicImpulseReceiverWithValue in list)
                {
                    dynamicImpulseReceiverWithValue.Trigger(value);
                }
                Pool.Return(ref list);
            }
        }

        public static IEnumerable<MethodBase> GenerateGenericMethodTargets(IEnumerable<Type> genericTypes, string methodName, params Type[] baseTypes)
        {
            return GenerateGenericMethodTargets(genericTypes, methodName, (IEnumerable<Type>)baseTypes);
        }

        public static IEnumerable<MethodBase> GenerateGenericMethodTargets(IEnumerable<Type> genericTypes, string methodName, IEnumerable<Type> baseTypes)
        {
            return GenerateMethodTargets(methodName,
                genericTypes.SelectMany(type => baseTypes.Select(baseType => baseType.MakeGenericType(type))));
        }

        public static IEnumerable<MethodBase> GenerateMethodTargets(string methodName, params Type[] baseTypes)
        {
            return GenerateMethodTargets(methodName, (IEnumerable<Type>)baseTypes);
        }

        public static IEnumerable<MethodBase> GenerateMethodTargets(string methodName, IEnumerable<Type> baseTypes)
        {
            return baseTypes.Select(type => type.GetMethod(methodName, AccessTools.allDeclared)).Where(m => m != null);
        }

        private static bool GenericTypesFilter(Type type)
        {
            return (!type.IsNested && type.IsGenericType)
                   || (type.IsNested && (type.IsGenericType || GenericTypesFilter(type.DeclaringType)));
        }

        private static bool NoGenericTypesFilter(Type type)
        {
            return !GenericTypesFilter(type);
        }

        private static bool PublicTypesFilter(Type type)
        {
            return (!type.IsNested && type.IsPublic)
                   || (type.IsNested && type.IsNestedPublic && PublicTypesFilter(type.DeclaringType));
        }
    }
}



//static IEnumerable<MethodBase> TargetMethods()
//{
//    var type = from asm in AppDomain.CurrentDomain.GetAssemblies().AsParallel() //some c# bs faolan likes
//        from ti in asm.GetTypes()
//        where typeof(IWorldElement).IsAssignableFrom(ti)
//        select ti;
//    foreach (var t in type)
//    {
//        yield return AccessTools.DeclaredPropertySetter(typeof(DynamicImpulseTriggerWithValue<>).MakeGenericType(t), "Run");
//    }
//}