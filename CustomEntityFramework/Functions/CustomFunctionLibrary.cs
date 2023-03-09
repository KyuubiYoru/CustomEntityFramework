using BaseX;
using FrooxEngine;
using FrooxEngine.LogiX.ProgramFlow;
using HarmonyLib;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace CustomEntityFramework.Functions
{
    public static class CustomFunctionLibrary
    {
        public const string DynamicImpulseTagPrefix = DynamicVariableSpaceName + ".";
        public const string DynamicVariableSpaceName = "CF";

        private static readonly Type bareInternalLibraryType = typeof(InternalLibrary<>);
        private static readonly Dictionary<Type, MethodInfo> genericInvokeFunctions = new();
        private static readonly Dictionary<string, Action> registeredFunctions = new();

        public static T InvokeFunction<T>(string name, DynamicImpulseTriggerWithValue<T> dynImpulseTrigger)
        {
            try
            {
                var type = dynImpulseTrigger.GetType().GetGenericArguments()[0];

                if (type.IsValueType)
                    return InternalLibrary<T>.Invoke(name, dynImpulseTrigger);

                if (!genericInvokeFunctions.TryGetValue(type, out var method))
                {
                    method = bareInternalLibraryType.MakeGenericType(type).GetMethod(nameof(InternalLibrary<T>.Invoke));
                    genericInvokeFunctions.Add(type, method);
                }

                return (T)method.Invoke(null, new object[] { name, dynImpulseTrigger });
            }
            catch (Exception e)
            {
                CustomEntityFramework.Debug(e.Message);
                CustomEntityFramework.Debug(e);

                return default;
            }
        }

        public static void InvokeFunction(string name)
        {
            try
            {
                if (registeredFunctions.TryGetValue(name, out var action))
                    action();
                else
                    CustomEntityFramework.Warn($"Attempt to invoke custom function [{name}] which doesn't exist.");
            }
            catch (Exception e)
            {
                CustomEntityFramework.Debug(e.Message);
                CustomEntityFramework.Debug(e);
            }
        }

        public static void RegisterFunction(string name, Action action)
        {
            registeredFunctions.ThrowIfExists(name);
            registeredFunctions.Add(name, action);
        }

        public static void RegisterFunction(string name, Delegate @delegate)
        {
            InternalLibrary<Slot>.RegisterFunction(name, @delegate);
        }

        public static void RegisterFunction<T>(string name, Func<T, T> function) where T : struct
        {
            InternalLibrary<T>.RegisterFunction(name, function);
        }

        public static void RegisterFunction<T>(string name, Func<T> function) where T : struct
        {
            InternalLibrary<T>.RegisterFunction(name, function);
        }

        public static void RegisterFunction(string name, Func<string, string> function)
        {
            InternalLibrary<string>.RegisterFunction(name, function);
        }

        public static void RegisterFunction(string name, Func<string> function)
        {
            InternalLibrary<string>.RegisterFunction(name, function);
        }

        private static void ThrowIfExists(this Dictionary<string, Action> dictionary, string name)
        {
            if (dictionary.ContainsKey(name))
                throw new InvalidOperationException($"Function [{name}] is already used by [{dictionary[name].Method.FullDescription()}]");
        }

        private static void ThrowIfExists<T>(this Dictionary<string, FunctionWrapper<T>> dictionary, string name)
        {
            if (dictionary.ContainsKey(name))
                throw new InvalidOperationException($"Function [{name}] is already used by [{dictionary[name].Delegate.Method.FullDescription()}]");
        }

        private static class InternalLibrary<T>
        {
            private static readonly Dictionary<string, FunctionWrapper<T>> registeredFunctions = new();

            static InternalLibrary()
            {
                var type = typeof(T);

                if (!type.IsValueType && (type != typeof(string) && type != typeof(Slot)))
                    throw new InvalidOperationException($"Can't create instance of {nameof(InternalLibrary<T>)} with {type.Name}. Only structs, string and Slot are allowed.");
            }

            public static T Invoke(string name, DynamicImpulseTriggerWithValue<T> dynImpulseTrigger)
            {
                if (!registeredFunctions.TryGetValue(name, out var wrapper))
                {
                    CustomEntityFramework.Warn($"Attempt to invoke custom function [{name}<{typeof(T).Name}>] which doesn't exist.");
                    return dynImpulseTrigger.Value.Evaluate();
                }

                return wrapper.Invoke(dynImpulseTrigger);
            }

            public static void RegisterFunction(string name, Func<T, T> function)
            {
                registeredFunctions.ThrowIfExists(name);
                registeredFunctions.Add(name, new DynamicImpulseTriggerValueWrapper<T>(function));
            }

            public static void RegisterFunction(string name, Func<T> function)
            {
                registeredFunctions.ThrowIfExists(name);
                registeredFunctions.Add(name, new DynamicImpulseTriggerValueWrapper<T>((T _) => function()));
            }

            public static void RegisterFunction(string name, Delegate @delegate)
            {
                registeredFunctions.ThrowIfExists(name);
                registeredFunctions.Add(name, (FunctionWrapper<T>)(object)new DynamicVariableSpaceWrapper(@delegate));
            }
        }
    }
}