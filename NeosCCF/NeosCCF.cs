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
using FrooxEngine.LogiX;
using System.Collections;

namespace NeosCCF
{
    public class NeosCCF : NeosMod
    {
        public const string DynamicVariableSpaceName = "CCF";

        private static CallTargetManager _callTargetManager = new CallTargetManager();
        public override string Author => "KyuubiYoru";
        public override string Link => "https://github.com/KyuubiYoru/NeosCCF.git";
        public override string Name => "NeosCCF";
        public override string Version => "1.0.0";

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

        public override void OnEngineInit()
        {
            var harmony = new Harmony("net.KyuubiYoru.NeosCCF");
            //harmony.PatchAll();
            DynamicImpulseTriggerPatch.Patch(harmony);
            CustomFunctionLibrary.RegisterFunction("Version", () => Version);
            //_callTargetManager.RegisterCallTarget(typeof(DynamicImpulseTriggerWithValue<string>), "NeosCCF.version", new CallBackDelegate<string>(SendVersion));
        }

        public void SendVersion<T>(DynamicImpulseTriggerWithValue<T> value, string[] args)
        {
            SendImpulseString(true, value.TargetHierarchy.Evaluate(), "NeosCCF.version", Version);
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

        public delegate void CallBackDelegate<T>(DynamicImpulseTriggerWithValue<T> value, string[] args);

        private class DynamicImpulseTriggerPatch
        {
            internal static Type[] NeosPrimitiveAndEnumTypes;
            private const string CustomFunctionPrefix = "NeosCCF.";
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

                NeosPrimitiveAndEnumTypes = neosPrimitiveTypes.Concat(neosEnumTypes).ToArray();

                foreach (var type in NeosPrimitiveAndEnumTypes)
                {
                    var createdType = typeof(DynamicImpulseTriggerWithValue<>).MakeGenericType(type);
                    var methodInfo = createdType.GetMethod("Run", AccessTools.allDeclared);

                    harmony.Patch(methodInfo, new HarmonyMethod(prefix.MakeGenericMethod(type)));
                }
            }

            public static bool Prefix<T>(DynamicImpulseTriggerWithValue<T> __instance)
            {
                if (!__instance.Enabled)
                    return false;

                Msg("Prefix Type: " + typeof(T));
                Msg("Instance Type: " + __instance.GetType());
                Msg("Instance TargetType: " + __instance.Value.TargetType);
                //Msg("Nested Instance Types: ");
                //foreach (var type in __instance.GetType().GetNestedTypes())
                //{
                //    Msg(type.ToString());
                //}

                var tag = __instance.Tag.Evaluate();
                var target = __instance.TargetHierarchy.Evaluate();
                var excludeDisabled = __instance.ExcludeDisabled.Evaluate();

                T value = default;
                if (!tag.StartsWith(CustomFunctionPrefix))
                {
                    value = __instance.Value.Evaluate();
                }
                else
                {
                    var name = tag.Remove(0, CustomFunctionPrefix.Length);
                    CustomFunctionLibrary.InvokeFunction(name, __instance);

                    //var value = __instance.Value.Evaluate();
                    //string result = value + " type: " + value.GetType();
                    //_callTargetManager.InvokeCallTarget<T>(tag, __instance);

                    //SendImpulseString(excludeDisabled, target, "echo", result);
                }

                //Run original code
                if (target != null)
                {
                    var list = Pool.BorrowList<DynamicImpulseReceiverWithValue<T>>();
                    target.GetComponentsInChildren(list, r => r.Tag.Evaluate() == tag, excludeDisabled);

                    foreach (DynamicImpulseReceiverWithValue<T> dynamicImpulseReceiverWithValue in list)
                        dynamicImpulseReceiverWithValue.Trigger(value);

                    Pool.Return(ref list);

                    //var instanceType = __instance.GetType().GenericTypeArguments;
                    //var actualType = typeof(DynamicImpulseTriggerWithValue<>).MakeGenericType(instanceType);
                    //var runMethod = actualType.GetMethod(nameof(DynamicImpulseTriggerWithValue<T>.Run), AccessTools.all);

                    //var makeFilterMethod = typeof(DynamicImpulseTriggerPatch).GetMethod(nameof(makeImpulseReceiverFilter), AccessTools.all).MakeGenericMethod(instanceType);
                    //var filterMethod = makeFilterMethod.Invoke(null, new[] { tag });

                    //var targetType = typeof(DynamicImpulseReceiverWithValue<>).MakeGenericType(instanceType);
                    //var getTargets = typeof(Slot).GetMethods().First(m => m.Name == nameof(Slot.GetComponentsInChildren) && m.GetParameters().Length == 3).MakeGenericMethod(targetType);
                    //var result = (IEnumerable)getTargets.Invoke(target, new object[] { filterMethod, excludeDisabled, false });

                    //foreach (var item in result)
                    //    runMethod.Invoke(item, new object[] { value });
                }

                __instance.OnTriggered.Trigger();

                return false;
            }

            private static Predicate<DynamicImpulseReceiverWithValue<T>> makeImpulseReceiverFilter<T>(string tag)
                => receiver => receiver.Tag.Evaluate() == tag;
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