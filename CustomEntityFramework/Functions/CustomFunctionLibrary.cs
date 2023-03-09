using BaseX;
using FrooxEngine;
using FrooxEngine.LogiX.ProgramFlow;
using HarmonyLib;
using System;
using System.Collections;
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
        private static readonly Dictionary<string, Action> registeredActions = new();

        public static bool ActionRegistered(string name)
        {
            return registeredActions.ContainsKey(name);
        }

        public static void ClearAction(string name)
        {
            registeredActions.Remove(name);
        }

        public static void ClearFunction<T>(string name)
        {
            InternalLibrary<T>.ClearFunction(name);
        }

        public static void ClearFunction(string name)
        {
            InternalLibrary<Slot>.ClearFunction(name);
        }

        public static bool FunctionRegistered(string name)
        {
            return InternalLibrary<Slot>.FunctionRegistered(name);
        }

        public static bool FunctionRegistered<T>(string name)
        {
            return InternalLibrary<T>.FunctionRegistered(name);
        }

        public static void InvokeAction(string name)
        {
            try
            {
                if (registeredActions.TryGetValue(name, out var action))
                    action();
                else
                    CustomEntityFramework.Warn($"Attempt to invoke custom action [{name}] which doesn't exist.");
            }
            catch (Exception e)
            {
                CustomEntityFramework.Debug(e.Message);
                CustomEntityFramework.Debug(e);
            }
        }

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

        public static void RegisterAction(string name, Action action)
        {
            ThrowIfExists(name);
            registeredActions.Add(name, action);
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

        private static void ThrowIfExists(string name)
        {
            if (registeredActions.TryGetValue(name, out var action))
                throw new InvalidOperationException($"Function [{name}] is already registered by [{action.Method.FullDescription()}]");
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

            public static void ClearFunction(string name)
            {
                registeredFunctions.Remove(name);
            }

            public static bool FunctionRegistered(string name)
            {
                return registeredFunctions.ContainsKey(name);
            }

            public static T Invoke(string name, DynamicImpulseTriggerWithValue<T> dynImpulseTrigger)
            {
                if (!registeredFunctions.TryGetValue(name, out var wrapper))
                {
                    CustomEntityFramework.Warn($"Attempt to invoke custom function [{name}<{typeof(T).Name}>] which doesn't exist.");
                    dynImpulseTrigger.Value.Evaluate();
                    return default;
                }

                return wrapper.Invoke(dynImpulseTrigger);
            }

            public static void RegisterFunction(string name, Func<T, T> function)
            {
                ThrowIfExists(name);
                registeredFunctions.Add(name, new DynamicImpulseTriggerValueWrapper<T>(function));
            }

            public static void RegisterFunction(string name, Func<T> function)
            {
                ThrowIfExists(name);
                registeredFunctions.Add(name, new DynamicImpulseTriggerValueWrapper<T>((T _) => function()));
            }

            public static void RegisterFunction(string name, Delegate @delegate)
            {
                ThrowIfExists(name);
                registeredFunctions.Add(name, (FunctionWrapper<T>)(object)new DynamicVariableSpaceWrapper(@delegate));
            }

            private static void ThrowIfExists(string name)
            {
                if (registeredFunctions.TryGetValue(name, out var wrapper))
                    throw new InvalidOperationException($"Function [{name}] is already registered by [{wrapper.Delegate.Method.FullDescription()}]");
            }
        }
    }
}