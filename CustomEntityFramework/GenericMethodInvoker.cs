﻿using System;
using System.Collections.Generic;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace CustomEntityFramework
{
    internal sealed class GenericMethodInvoker<TInstance, TReturn>
    {
        private readonly Dictionary<TypeDefinition, MethodInfo> concreteMethods = new();

        public MethodInfo GenericMethod { get; }

        public GenericMethodInvoker(MethodInfo method)
        {
            // Intentionally left out to simplify the GenericTypeMethodInvoker
            //if (!method.ContainsGenericParameters)
            //    throw new InvalidOperationException("Target method must have remaining open type parameters.");

            GenericMethod = method;
        }

        public GenericMethodInvoker(Delegate @delegate) : this(@delegate.Method)
        { }

        public TReturn Invoke(TInstance instance, Type[] types, params object[] parameters)
        {
            return InvokeInternal(instance, types, parameters);
        }

        public TReturn Invoke(TInstance instance, Type type, params object[] parameters)
        {
            return InvokeInternal(instance, type, parameters);
        }

        internal TReturn InvokeInternal(TInstance instance, TypeDefinition definition, object[] parameters)
        {
            if (!concreteMethods.TryGetValue(definition, out var method))
            {
                if (GenericMethod.ContainsGenericParameters)
                    method = GenericMethod.MakeGenericMethod(definition.Types);
                else
                    method = GenericMethod;

                concreteMethods.Add(definition, method);
            }

            return (TReturn)method.Invoke(instance, parameters);
        }
    }

    internal sealed class GenericMethodInvoker<TReturn>
    {
        private readonly Dictionary<TypeDefinition, MethodInfo> concreteMethods = new();

        public MethodInfo GenericMethod { get; }

        public GenericMethodInvoker(MethodInfo method)
        {
            // Intentionally left out to simplify the GenericTypeMethodInvoker
            //if (!method.ContainsGenericParameters)
            //    throw new InvalidOperationException("Target method must have remaining open type parameters.");

            GenericMethod = method;
        }

        public GenericMethodInvoker(Delegate @delegate) : this(@delegate.Method)
        { }

        public TReturn Invoke(Type[] types, params object[] parameters)
        {
            return InvokeInternal(types, parameters);
        }

        public TReturn Invoke(Type type, params object[] parameters)
        {
            return InvokeInternal(type, parameters);
        }

        private TReturn InvokeInternal(TypeDefinition definition, object[] parameters)
        {
            if (!concreteMethods.TryGetValue(definition, out var method))
            {
                if (GenericMethod.ContainsGenericParameters)
                    method = GenericMethod.MakeGenericMethod(definition.Types);
                else
                    method = GenericMethod;

                concreteMethods.Add(definition, method);
            }

            return (TReturn)method.Invoke(null, parameters);
        }
    }

    internal sealed class GenericMethodInvoker
    {
        private readonly Dictionary<TypeDefinition, MethodInfo> concreteMethods = new();

        public MethodInfo GenericMethod { get; }

        public GenericMethodInvoker(MethodInfo method)
        {
            // Intentionally left out to simplify the GenericTypeMethodInvoker
            //if (!method.ContainsGenericParameters)
            //    throw new InvalidOperationException("Target method must have remaining open type parameters.");

            GenericMethod = method;
        }

        public GenericMethodInvoker(Delegate @delegate) : this(@delegate.Method)
        { }

        public void Invoke(Type[] types, params object[] parameters)
        {
            InvokeInternal(types, parameters);
        }

        public void Invoke(Type type, params object[] parameters)
        {
            InvokeInternal(type, parameters);
        }

        private void InvokeInternal(TypeDefinition definition, object[] parameters)
        {
            if (!concreteMethods.TryGetValue(definition, out var method))
            {
                if (GenericMethod.ContainsGenericParameters)
                    method = GenericMethod.MakeGenericMethod(definition.Types);
                else
                    method = GenericMethod;

                concreteMethods.Add(definition, method);
            }

            method.Invoke(null, parameters);
        }
    }
}