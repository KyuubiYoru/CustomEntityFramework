using BaseX;
using FrooxEngine;
using FrooxEngine.LogiX.ProgramFlow;
using HarmonyLib;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

namespace CustomEntityFramework.Functions
{
    internal sealed class DynamicVariableSpaceWrapper : FunctionWrapper<Slot>
    {
        public const string SlotParameterName = "_slot";
        public const string SpaceParameterName = "_space";

        public static readonly Type SlotParameterType = typeof(Slot);
        public static readonly Type SpaceParameterType = typeof(DynamicVariableSpace);
        private static readonly Type voidType = typeof(void);
        private readonly Parameter[] parameterWrappers;
        public bool HasSlotParameter { get; }

        public bool HasSpaceParameter { get; }

        public int Parameters => parameterWrappers.Length;
        public bool RequiresSlot => HasSlotParameter || RequiresSpace;
        public bool RequiresSpace { get; }
        public bool UseResult { get; }
        public bool WriteBack { get; } = false;

        public DynamicVariableSpaceWrapper(Delegate @delegate)
            : base(@delegate)
        {
            if (@delegate.Method.ReturnType == SlotParameterType)
                UseResult = true;
            else if (@delegate.Method.ReturnType != voidType)
                throw new InvalidOperationException($"Return Type of method [{@delegate.Method.FullDescription()}] using {nameof(DynamicVariableSpaceWrapper)} must be void or Slot!");

            var parameters = @delegate.Method.GetParameters();
            var wrappers = new List<Parameter>(parameters.Length);

            for (var i = 0; i < parameters.Length; ++i)
            {
                var parameter = parameters[i];

                if (parameter.Name == SlotParameterName)
                {
                    if (parameter.ParameterType != SlotParameterType)
                        throw new InvalidOperationException($"Found special Slot parameter name [{SlotParameterName}] with wrong Type [{parameter.ParameterType}] on potential custom method {@delegate.Method.FullDescription()}");

                    HasSlotParameter = true;
                    wrappers.Add(new SlotParameter(parameter));
                    continue;
                }

                if (parameter.Name == SpaceParameterName)
                {
                    if (parameter.ParameterType != SpaceParameterType)
                        throw new InvalidOperationException($"Found special DynamicVariableSpace parameter name [{SlotParameterName}] with wrong Type [{parameter.ParameterType}] on potential custom method {@delegate.Method.FullDescription()}");

                    HasSpaceParameter = true;
                    RequiresSpace = true;
                    wrappers.Add(new SpaceParameter(parameter));
                    continue;
                }

                var wrapper = new VariableAccessParameter(parameter);
                WriteBack |= wrapper.WriteBack;
                wrappers.Add(wrapper);
                RequiresSpace = true;
            }

            parameterWrappers = wrappers.ToArray();
        }

        public override bool Invoke(DynamicImpulseTriggerWithValue<Slot> dynImpulseTrigger, out Slot result)
        {
            var spaceSlot = dynImpulseTrigger.Value.Evaluate();
            result = spaceSlot;

            if (RequiresSlot && spaceSlot == null)
            {
                CustomEntityFramework.Warn($"Attempt to call [{Delegate.Method.FullDescription()}] as a custom function without the necessary Slot as the value.");
                return false;
            }

            if (spaceSlot.GetComponent<DynamicVariableSpace>(matchSpace) is not DynamicVariableSpace space)
            {
                if (RequiresSpace)
                {
                    CustomEntityFramework.Warn($"Attempt to call [{Delegate.Method.FullDescription()}] as a custom function without a proper {CustomFunctionLibrary.DynamicVariableSpaceName}-DynamicVariableSpace as the value.");
                    return false;
                }

                space = null;
            }

            var errors = false;
            var parameters = new object[Parameters];

            for (var i = 0; i < Parameters; ++i)
            {
                var wrapper = parameterWrappers[i];

                if (!wrapper.WriteOnly && !wrapper.TryReadValue(spaceSlot, space, out parameters[i]))
                {
                    errors = true;
                    CustomEntityFramework.Warn($"Attempt to call [{Delegate.Method.FullDescription()}] as a custom function while the non-optional parameter [{wrapper}] is missing on the DynamicVariableSpace.");
                }
            }

            if (errors)
                return false;

            if (UseResult)
                result = (Slot)Delegate.DynamicInvoke(parameters);
            else
                Delegate.DynamicInvoke(parameters);

            if (WriteBack)
            {
                for (var i = 0; i < Parameters; ++i)
                {
                    var wrapper = parameterWrappers[i];

                    if (!wrapper.WriteBack || wrapper.ReadOnly)
                        continue;

                    wrapper.TryWriteValue(space, parameters[i]);
                }
            }

            return true;
        }

        private static bool matchSpace(DynamicVariableSpace space)
            => space.SpaceName.Value == CustomFunctionLibrary.DynamicVariableSpaceName;

        private delegate T TryGetValue<T>(string name, out T value);

        private abstract class Parameter
        {
            public readonly object DefaultValue;
            public readonly bool IsOptional;
            public readonly string Name;
            public readonly Type Type;
            public readonly bool WriteBack;
            public readonly bool WriteOnly;
            public abstract bool ReadOnly { get; }

            public Parameter(ParameterInfo parameter)
            {
                Type = parameter.ParameterType;
                Name = parameter.Name;
                WriteBack = parameter.ParameterType.IsByRef;
                WriteOnly = WriteBack && parameter.IsOut;
                IsOptional = parameter.IsOptional;
                DefaultValue = parameter.HasDefaultValue ? parameter.DefaultValue : parameter.ParameterType.GetDefaultValue();
            }

            public override string ToString() => $"{Type} {Name}";

            public abstract bool TryReadValue(Slot slot, DynamicVariableSpace space, out object value);

            public abstract bool TryWriteValue(DynamicVariableSpace space, object value);
        }

        private sealed class SlotParameter : Parameter
        {
            public override bool ReadOnly { get; } = true;

            public SlotParameter(ParameterInfo parameter)
                : base(parameter)
            { }

            public override bool TryReadValue(Slot slot, DynamicVariableSpace space, out object value)
            {
                value = slot;
                return true;
            }

            public override bool TryWriteValue(DynamicVariableSpace space, object value)
            {
                return false;
            }
        }

        private sealed class SpaceParameter : Parameter
        {
            public override bool ReadOnly { get; } = true;

            public SpaceParameter(ParameterInfo parameter)
                : base(parameter)
            { }

            public override bool TryReadValue(Slot slot, DynamicVariableSpace space, out object value)
            {
                value = space;
                return true;
            }

            public override bool TryWriteValue(DynamicVariableSpace space, object value)
            {
                return false;
            }
        }

        private sealed class VariableAccessParameter : Parameter
        {
            private static readonly MethodInfo bareReadMethod = typeof(DynamicVariableSpace).GetMethod(nameof(DynamicVariableSpace.TryReadValue));
            private static readonly MethodInfo bareWriteMethod = typeof(DynamicVariableSpace).GetMethod(nameof(DynamicVariableSpace.TryWriteValue));
            private static readonly Dictionary<Type, MethodInfo> readMethodCache = new();
            private static readonly Dictionary<Type, MethodInfo> writeMethodCache = new();
            private readonly MethodInfo readMethod;
            private readonly MethodInfo writeMethod;

            public override bool ReadOnly { get; } = false;

            public VariableAccessParameter(ParameterInfo parameter)
                : base(parameter)
            {
                if (!readMethodCache.TryGetValue(Type, out readMethod))
                {
                    readMethod = bareReadMethod.MakeGenericMethod(Type);
                    readMethodCache.Add(Type, readMethod);
                }

                if (!writeMethodCache.TryGetValue(Type, out writeMethod))
                {
                    writeMethod = bareWriteMethod.MakeGenericMethod(Type);
                    writeMethodCache.Add(Type, writeMethod);
                }
            }

            public override bool TryReadValue(Slot slot, DynamicVariableSpace space, out object value)
            {
                var parameters = new object[] { Name, null };

                var success = (bool)readMethod.Invoke(space, parameters);

                if (!success && IsOptional)
                {
                    value = DefaultValue;
                    return true;
                }

                value = parameters[1];
                return success;
            }

            public override bool TryWriteValue(DynamicVariableSpace space, object value)
            {
                return (bool)writeMethod.Invoke(space, new object[] { Name, value });
            }
        }
    }
}