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
    /// <summary>
    /// Contains methods to register actions and functions which can be invoked by dynamic impulses.
    /// </summary>
    public static class CustomFunctionLibrary
    {
        public const string DynamicImpulseTagPrefix = DynamicVariableSpaceName + ".";
        public const string DynamicVariableSpaceName = "CF";

        private static readonly GenericTypeMethodsInvoker genericInternalLibraryInvoker = new(typeof(InternalLibrary<>));
        private static readonly MethodInfo internalInvokeMethod = typeof(InternalLibrary<>).GetMethod("Invoke");
        private static readonly Dictionary<string, Action> registeredActions = new();

        /// <summary>
        /// Checks whether an action with the given name is already registered.
        /// </summary>
        /// <param name="name">The name to check.</param>
        /// <returns>Whether an action with the given name is already registered.</returns>
        public static bool ActionRegistered(string name)
        {
            return registeredActions.ContainsKey(name);
        }

        /// <summary>
        /// Removes the action with the given name, if it exists.
        /// </summary>
        /// <param name="name">The name of the action to remove.</param>
        public static void ClearAction(string name)
        {
            registeredActions.Remove(name);
        }

        /// <summary>
        /// Removes the <typeparamref name="T"/>-specific function with the given name, if it exists.
        /// </summary>
        /// <typeparam name="T">The function's type.</typeparam>
        /// <param name="name">The name of the <typeparamref name="T"/>-specific function to remove.</param>
        public static void ClearFunction<T>(string name)
        {
            InternalLibrary<T>.ClearFunction(name);
        }

        /// <summary>
        /// Removes the generic <see cref="Delegate"/> function with the given name, if it exists.
        /// </summary>
        /// <param name="name">The name of the <typeparamref name="T"/>-specific function to remove.</param>
        public static void ClearFunction(string name)
        {
            InternalLibrary<Slot>.ClearFunction(name);
        }

        /// <summary>
        /// Checks whether a generic <see cref="Delegate"/> function with the given name is already registered.
        /// </summary>
        /// <param name="name">The name to check.</param>
        /// <returns>Whether a function with the given name is already registered.</returns>
        public static bool FunctionRegistered(string name)
        {
            return InternalLibrary<Slot>.FunctionRegistered(name);
        }

        /// <summary>
        /// Checks whether a <typeparamref name="T"/>-specific function with the given name is already registered.
        /// </summary>
        /// <typeparam name="T">The function's type.</typeparam>
        /// <param name="name">The name to check.</param>
        /// <returns>Whether a <typeparamref name="T"/>-specific function with the given name is already registered.</returns>
        public static bool FunctionRegistered<T>(string name)
        {
            return InternalLibrary<T>.FunctionRegistered(name);
        }

        /// <summary>
        /// Tries to invoke the action with the given name.
        /// </summary>
        /// <param name="name">The name of the action to invoke.</param>
        /// <returns>Whether the invocation was successful.</returns>
        public static bool InvokeAction(string name)
        {
            try
            {
                if (registeredActions.TryGetValue(name, out var action))
                {
                    action();
                    return true;
                }
                else
                    CustomEntityFramework.Warn($"Attempt to invoke custom action [{name}] which doesn't exist.");
            }
            catch (Exception e)
            {
                CustomEntityFramework.Msg(e.Message);
                CustomEntityFramework.Msg(e);
            }

            return false;
        }

        /// <summary>
        /// Tries to invoke the function with the given name.
        /// </summary>
        /// <typeparam name="T">The function's type. Slot for generic <see cref="Delegate"/> functions.</typeparam>
        /// <param name="name">The name of the function to invoke.</param>
        /// <param name="dynImpulseTrigger">The <see cref="DynamicImpulseTriggerWithValue{T}"/> triggering the invocation.</param>
        /// <param name="result">The input value, or the result / potentially modified input value of the invocation.</param>
        /// <returns>Whether the invocation was successful.</returns>
        public static bool InvokeFunction<T>(string name, DynamicImpulseTriggerWithValue<T> dynImpulseTrigger, out T result)
        {
            try
            {
                var type = dynImpulseTrigger.GetType().GetGenericArguments()[0];

                if (type.IsValueType)
                    return InternalLibrary<T>.Invoke(name, dynImpulseTrigger, out result);

                var parameters = new object[] { name, dynImpulseTrigger, default(T) };
                var success = genericInternalLibraryInvoker.Invoke<bool>(internalInvokeMethod, type, parameters);

                result = (T)parameters[2];
                return success;
            }
            catch (Exception e)
            {
                CustomEntityFramework.Msg(e.Message);
                CustomEntityFramework.Msg(e);

                result = default;
                return false;
            }
        }

        /// <summary>
        /// Registers the given action for the given name.
        /// </summary>
        /// <param name="name">The name to register the action for.</param>
        /// <param name="action">The action for the name.</param>
        public static void RegisterAction(string name, Action action)
        {
            ThrowIfExists(name);
            registeredActions.Add(name, action);
        }

        /// <summary>
        /// Registers the given generic <see cref="Delegate"/> function for the given name.
        /// </summary>
        /// <param name="name">The name to register the function for.</param>
        /// <param name="delegate">The generic <see cref="Delegate"/> function for the name.</param>
        public static void RegisterFunction(string name, Delegate @delegate)
        {
            InternalLibrary<Slot>.RegisterFunction(name, @delegate);
        }

        /// <summary>
        /// Registers the given <typeparamref name="T"/>-specific function for the given name.
        /// </summary>
        /// <typeparam name="T">The function's type.</typeparam>
        /// <param name="name">The name to register the function for.</param>
        /// <param name="function">The <typeparamref name="T"/>-specific function for the name.</param>
        public static void RegisterFunction<T>(string name, Func<T, T> function) where T : struct
        {
            InternalLibrary<T>.RegisterFunction(name, function);
        }

        /// <summary>
        /// Registers the given <typeparamref name="T"/>-specific, parameterless function for the given name.
        /// </summary>
        /// <typeparam name="T">The function's value-type.</typeparam>
        /// <param name="name">The name to register the function for.</param>
        /// <param name="function">The <typeparamref name="T"/>-specific, parameterless function for the name.</param>
        public static void RegisterFunction<T>(string name, Func<T> function) where T : struct
        {
            InternalLibrary<T>.RegisterFunction(name, function);
        }

        /// <summary>
        /// Registers the given string-specific function for the given name.
        /// </summary>
        /// <param name="name">The name to register the function for.</param>
        /// <param name="function">The string-specific function for the name.</param>
        public static void RegisterFunction(string name, Func<string, string> function)
        {
            InternalLibrary<string>.RegisterFunction(name, function);
        }

        /// <summary>
        /// Registers the given string-specific, parameterless function for the given name.
        /// </summary>
        /// <param name="name">The name to register the function for.</param>
        /// <param name="function">The string-specific, parameterless function for the name.</param>
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

            public static bool Invoke(string name, DynamicImpulseTriggerWithValue<T> dynImpulseTrigger, out T result)
            {
                if (!registeredFunctions.TryGetValue(name, out var wrapper))
                {
                    CustomEntityFramework.Warn($"Attempt to invoke custom function [{name}<{typeof(T).Name}>] which doesn't exist.");
                    result = dynImpulseTrigger.Value.Evaluate();
                    return false;
                }

                return wrapper.Invoke(dynImpulseTrigger, out result);
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