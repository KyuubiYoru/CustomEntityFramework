using HarmonyLib;
using NeosModLoader;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace CustomEntityFramework
{
    internal sealed class GenericTypeMethodsInvoker
    {
        private readonly Dictionary<TypeDefinition, ConcreteType> concreteTypes = new();

        public Type GenericType { get; }

        public Func<MethodInfo, Type, MethodInfo> GetGenericMethodOfConcreteType { get; }

        public GenericTypeMethodsInvoker(Type genericType)
            : this(genericType, GetGenericMethodOfConcreteTypeDefault)
        { }

        public GenericTypeMethodsInvoker(Type genericType, Func<MethodInfo, Type, MethodInfo> getGenericMethodOfConcreteType)
        {
            GenericType = genericType;
            GetGenericMethodOfConcreteType = getGenericMethodOfConcreteType;
        }

        public TReturn Invoke<TReturn>(MethodInfo method, Type[] instanceTypes, object instance = default, Type[] methodTypes = null, object[] parameters = null)
        {
            return (TReturn)Invoke(method, instanceTypes, instance, methodTypes, parameters);
        }

        public TReturn Invoke<TReturn>(MethodInfo method, Type[] instanceTypes, object instance = default, Type methodType = null, object[] parameters = null)
        {
            return (TReturn)Invoke(method, instanceTypes, instance, methodType, parameters);
        }

        public object Invoke(MethodInfo method, Type[] instanceTypes, object instance = null, Type[] methodTypes = null, object[] parameters = null)
        {
            return InvokeInternal(method, instanceTypes, instance, methodTypes, parameters);
        }

        public object Invoke(MethodInfo method, Type instanceType, object instance = null, Type[] methodTypes = null, object[] parameters = null)
        {
            return InvokeInternal(method, instanceType, instance, methodTypes, parameters);
        }

        public object Invoke(MethodInfo method, Type[] instanceTypes, object instance = null, Type methodType = null, object[] parameters = null)
        {
            return InvokeInternal(method, instanceTypes, instance, methodType, parameters);
        }

        public object Invoke(MethodInfo method, Type instanceType, object instance = null, Type methodType = null, object[] parameters = null)
        {
            return InvokeInternal(method, instanceType, instance, methodType, parameters);
        }

        public TReturn Invoke<TReturn>(MethodInfo method, Type instanceType, object instance = default, Type[] methodTypes = null, object[] parameters = null)
        {
            return (TReturn)InvokeInternal(method, instanceType, instance, methodTypes, parameters);
        }

        public TReturn Invoke<TReturn>(MethodInfo method, Type instanceType, object instance = default, Type methodType = null, object[] parameters = null)
        {
            return (TReturn)InvokeInternal(method, instanceType, instance, methodType, parameters);
        }

        public TReturn Invoke<TReturn>(MethodInfo method, Type instanceType, object instance = default, object[] parameters = null)
        {
            return Invoke<TReturn>(method, instanceType, instance, (Type)null, parameters);
        }

        public TReturn Invoke<TReturn>(MethodInfo method, Type instanceType, object[] parameters = null)
        {
            return Invoke<TReturn>(method, instanceType, null, (Type)null, parameters);
        }

        public object Invoke(MethodInfo method, Type[] instanceTypes, object instance = null, object[] parameters = null)
        {
            return Invoke(method, instanceTypes, instance, (Type)null, parameters);
        }

        public object Invoke(MethodInfo method, Type[] instanceTypes, object[] parameters = null)
        {
            return Invoke(method, instanceTypes, null, (Type)null, parameters);
        }

        private static MethodInfo GetGenericMethodOfConcreteTypeDefault(MethodInfo needleMethod, Type concreteType)
        {
            NeosMod.Debug($"Looking for: {needleMethod.ReturnType.Name} {needleMethod.Name}({string.Join(", ", needleMethod.GetParameters().Select(p => $"{p.ParameterType.Name} {p.Name}"))})");

            return concreteType.GetMethods(AccessTools.all)
                .Single(hayMethod =>
                {
                    NeosMod.Debug($"Testing: {hayMethod.ReturnType.Name} {hayMethod.Name}({string.Join(", ", hayMethod.GetParameters().Select(p => $"{p.ParameterType.Name} {p.Name}"))})");

                    if (hayMethod.Name != needleMethod.Name)
                        return false;

                    var needleParameters = needleMethod.GetParameters();
                    var hayParameters = hayMethod.GetParameters();

                    if (hayParameters.Length != needleParameters.Length)
                        return false;

                    for (var i = 0; i < needleParameters.Length; ++i)
                    {
                        //var needleParameter = needleParameters[i];
                        //var hayParameter = hayParameters[i];
                        //var checkType = (hayParameter.ParameterType.IsGenericParameter && needleParameter.ParameterType.IsGenericParameter)
                        //             || (!hayParameter.ParameterType.IsGenericParameter && !needleParameter.ParameterType.IsGenericParameter);

                        //NeosMod.Msg($"Comparing: {hayParameter.ParameterType} to {needleParameter.ParameterType} => {hayParameter.ParameterType.FullName == needleParameter.ParameterType.FullName}");

                        //if (checkType && hayParameter.ParameterType.FullName != needleParameter.ParameterType.FullName)
                        //    return false;

                        // TODO: Do a proper type check? lol
                        if (hayParameters[i].Name != needleParameters[i].Name)
                            return false;
                    }

                    return true;
                });
        }

        private object InvokeInternal(MethodInfo method, TypeDefinition instanceTypes, object instance, TypeDefinition methodTypes, object[] parameters)
        {
            if (!concreteTypes.TryGetValue(instanceTypes, out var concreteType))
            {
                concreteType = GenericType.MakeGenericType(instanceTypes.Types);
                concreteTypes.Add(instanceTypes, concreteType);
            }

            var methodInvoker = concreteType.GetMethodInvoker(method, GetGenericMethodOfConcreteType);

            return methodInvoker.InvokeInternal(instance, methodTypes, parameters);
        }

        private readonly struct ConcreteType
        {
            public readonly Dictionary<MethodInfo, GenericMethodInvoker<object, object>> MethodInvokers = new();
            public readonly Type Type;

            public ConcreteType(Type type)
            {
                Type = type;
            }

            public static implicit operator ConcreteType(Type type) => new(type);

            public GenericMethodInvoker<object, object> GetMethodInvoker(MethodInfo genericMethod, Func<MethodInfo, Type, MethodInfo> getMethod)
            {
                if (!MethodInvokers.TryGetValue(genericMethod, out var methodInvoker))
                {
                    methodInvoker = new GenericMethodInvoker<object, object>(getMethod(genericMethod, Type));
                    MethodInvokers.Add(genericMethod, methodInvoker);
                }

                return methodInvoker;
            }
        }
    }
}