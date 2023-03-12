using FrooxEngine.LogiX.ProgramFlow;
using HarmonyLib;
using NeosModLoader;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using FrooxEngine.LogiX;
using System.Collections;
using CustomEntityFramework.Functions;
using BaseX;
using CloudX.Shared;
using FrooxEngine.LogiX.Input;
using FrooxEngine;

namespace CustomEntityFramework
{
    public class CustomEntityFramework : NeosMod
    {
        internal static readonly Type[] NeosPrimitiveAndEnumTypes;
        public override string Author => "Banane9, KyuubiYoru";
        public override string Link => "https://github.com/KyuubiYoru/NeosCCF.git";
        public override string Name => "CustomEntityFramework";
        public override string Version => "1.0.0";

        static CustomEntityFramework()
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

        public override void OnEngineInit()
        {
            var harmony = new Harmony("net.KyuubiYoru.CustomEntityFramework");
            DynamicImpulseTriggerWithValuePatch.Patch(harmony);
            harmony.PatchAll();

            CustomFunctionLibrary.RegisterFunction("Version", () => Version);
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

        public delegate void CallBackDelegate<T>(DynamicImpulseTriggerWithValue<T> value, string[] args);
    }
}