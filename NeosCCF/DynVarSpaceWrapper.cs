using BaseX;
using FrooxEngine;
using FrooxEngine.LogiX.ProgramFlow;
using HarmonyLib;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

namespace NeosCCF
{
    internal sealed class DynVarSpaceWrapper : FunctionWrapper<Slot>
    {
        public const string SlotParameterName = "_slot";
        public const string SpaceParameterName = "_space";

        public static readonly Type SlotParameterType = typeof(Slot);
        public static readonly Type SpaceParameterType = typeof(DynamicVariableSpace);
        private static readonly Type voidType = typeof(void);
        private readonly AccessWrapper[] parameterWrappers;
        private readonly int slotParameterIndex = -1;
        private readonly int spaceParameterIndex = -1;
        private readonly object target;
        public bool HasSlotParameter => slotParameterIndex >= 0;

        public bool HasSpaceParameter => spaceParameterIndex >= 0;

        public int Parameters => parameterWrappers.Length;
        public bool RequiresSlot => HasSlotParameter || RequiresSpace;
        public bool RequiresSpace => HasSpaceParameter || parameterWrappers.Length > 0;
        public bool UseResult { get; }
        public bool WriteBack { get; } = false;

        public DynVarSpaceWrapper(Delegate @delegate)
            : base(@delegate)
        {
            target = @delegate.Target;

            if (@delegate.Method.ReturnType == SlotParameterType)
                UseResult = true;
            else if (@delegate.Method.ReturnType != voidType)
                throw new InvalidOperationException($"Return Type of method [{@delegate.Method.FullDescription()}] using {nameof(DynVarSpaceWrapper)} must be void or Slot!");

            var parameters = @delegate.Method.GetParameters();
            var wrappers = new List<AccessWrapper>(parameters.Length);

            for (var i = 0; i < parameters.Length; ++i)
            {
                var parameter = parameters[i];

                if (parameter.Name == SlotParameterName)
                {
                    if (parameter.ParameterType != SlotParameterType)
                        throw new InvalidOperationException($"Found special Slot parameter name [{SlotParameterName}] with wrong Type [{parameter.ParameterType}] on potential custom method {@delegate.Method.FullDescription()}");

                    slotParameterIndex = i;
                    wrappers.Add(default);

                    continue;
                }

                if (parameter.Name == SpaceParameterName)
                {
                    if (parameter.ParameterType != SpaceParameterType)
                        throw new InvalidOperationException($"Found special DynamicVariableSpace parameter name [{SlotParameterName}] with wrong Type [{parameter.ParameterType}] on potential custom method {@delegate.Method.FullDescription()}");

                    spaceParameterIndex = i;
                    wrappers.Add(default);

                    continue;
                }

                wrappers.Add(new AccessWrapper(parameter));
                WriteBack |= parameter.ParameterType.IsByRef;
            }

            parameterWrappers = wrappers.ToArray();
        }

        public override Slot Invoke(DynamicImpulseTriggerWithValue<Slot> dynImpulseTrigger)
        {
            var spaceSlot = dynImpulseTrigger.Value.Evaluate();

            if (RequiresSlot && spaceSlot == null)
            {
                NeosCCF.Warn($"Attempt to call [{Delegate.Method.FullDescription()}] as a custom function without the necessary Slot as the value.");
                return null;
            }

            if (spaceSlot.GetComponent<DynamicVariableSpace>(matchSpace) is not DynamicVariableSpace space)
            {
                if (RequiresSpace)
                {
                    NeosCCF.Warn($"Attempt to call [{Delegate.Method.FullDescription()}] as a custom function without a proper {NeosCCF.DynamicVariableSpaceName}-DynamicVariableSpace as the value.");

                    return null;
                }

                space = null;
            }

            var errors = false;
            var parameters = new object[Parameters + 1];
            parameters[slotParameterIndex + 1] = spaceSlot;
            parameters[spaceParameterIndex + 1] = space;
            parameters[0] = target;

            for (var i = 0; i < Parameters; ++i)
            {
                var wrapper = parameterWrappers[i];

                if (i == slotParameterIndex || i == spaceParameterIndex)
                    continue;

                if (!wrapper.WriteOnly && !wrapper.TryReadValue(space, out parameters[i + 1]))
                {
                    if (wrapper.IsOptional)
                    {
                        parameters[i + 1] = wrapper.DefaultValue;
                        continue;
                    }

                    errors = true;
                    NeosCCF.Warn($"Attempt to call [{Delegate.Method.FullDescription()}] as a custom function while the non-optional parameter [{parameterWrappers}] is missing on the DynamicVariableSpace.");
                }
            }

            if (errors)
                return null;

            var result = Delegate.DynamicInvoke(parameters);

            if (WriteBack)
            {
                for (var i = 0; i < Parameters; ++i)
                {
                    var wrapper = parameterWrappers[i];

                    if (i == slotParameterIndex || i == spaceParameterIndex || !wrapper.WriteBack)
                        continue;

                    wrapper.TryWriteValue(space, parameters[i + 1]);
                }
            }

            return UseResult ? (Slot)result : spaceSlot;
        }

        private static bool matchSpace(DynamicVariableSpace space)
            => space.SpaceName.Value == NeosCCF.DynamicVariableSpaceName;

        private delegate T TryGetValue<T>(string name, out T value);

        private readonly struct AccessWrapper
        {
            public readonly object DefaultValue;
            public readonly bool IsOptional;
            public readonly string Name;
            public readonly Type Type;
            public readonly bool WriteBack;
            public readonly bool WriteOnly;
            private static readonly MethodInfo bareReadMethod = typeof(DynamicVariableSpace).GetMethod(nameof(DynamicVariableSpace.TryReadValue));
            private static readonly MethodInfo bareWriteMethod = typeof(DynamicVariableSpace).GetMethod(nameof(DynamicVariableSpace.TryWriteValue));

            private static readonly Dictionary<Type, MethodInfo> readMethodCache = new();
            private static readonly Dictionary<Type, MethodInfo> writeMethodCache = new();

            private readonly MethodInfo readMethod;
            private readonly MethodInfo writeMethod;

            public AccessWrapper(Type type, string name, bool writeBack, bool writeOnly, bool isOptional, object defaultValue)
            {
                Type = type;
                Name = name;
                WriteBack = writeBack;
                WriteOnly = writeOnly;
                IsOptional = isOptional;
                DefaultValue = defaultValue;

                if (!readMethodCache.TryGetValue(type, out readMethod))
                {
                    readMethod = bareReadMethod.MakeGenericMethod(type);
                    readMethodCache.Add(type, readMethod);
                }

                if (!writeMethodCache.TryGetValue(type, out writeMethod))
                {
                    writeMethod = bareWriteMethod.MakeGenericMethod(type);
                    writeMethodCache.Add(type, writeMethod);
                }
            }

            public AccessWrapper(ParameterInfo parameter)
                : this(parameter.ParameterType, parameter.Name,
                      parameter.ParameterType.IsByRef,
                      parameter.ParameterType.IsByRef && parameter.IsOut,
                      parameter.IsOptional,
                      parameter.HasDefaultValue ? parameter.DefaultValue : parameter.ParameterType.GetDefaultValue())
            { }

            public override string ToString() => $"{Type} {Name}";

            public bool TryReadValue(DynamicVariableSpace space, out object value)
            {
                var parameters = new object[] { Name, null };

                var success = (bool)readMethod.Invoke(space, parameters);

                value = parameters[1];
                return success;
            }

            public bool TryWriteValue(DynamicVariableSpace space, object value)
            {
                return (bool)writeMethod.Invoke(space, new object[] { Name, value });
            }
        }
    }
}