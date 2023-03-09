using FrooxEngine;
using HarmonyLib;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace CustomEntityFramework
{
    public abstract class DynamicVariable
    {
        private static readonly Type bareDynamicVariableType = typeof(DynamicVariable<>);
        private static readonly Dictionary<Type, ConstructorInfo> genericDynamicVariableConstructors = new();
        public string Name { get; }

        public abstract Type Type { get; }

        public object Value
        {
            get => getValue();
            set => setValue(value);
        }

        internal DynamicVariable(string name)
        {
            Name = name;
        }

        public static DynamicVariable Create(DynamicVariableIdentity identity, DynamicVariableSpace.ValueManager manager)
        {
            if (!genericDynamicVariableConstructors.TryGetValue(identity.Type, out var constructor))
            {
                constructor = bareDynamicVariableType.MakeGenericType(identity.Type).GetConstructors().First(ctr => ctr.GetParameters().Length == 1);
                genericDynamicVariableConstructors.Add(identity.Type, constructor);
            }

            return (DynamicVariable)constructor.Invoke(new object[] { manager });
        }

        protected abstract object getValue();

        protected abstract void setValue(object value);
    }

    public sealed class DynamicVariable<T> : DynamicVariable
    {
        private readonly DynamicVariableSpace.ValueManager<T> valueManager;

        public int ReadableValues => valueManager.ReadableValueCount;
        public override Type Type { get; } = typeof(T);

        public new T Value
        {
            get => valueManager.Value;
            set => valueManager.SetValue(value);
        }

        public int Values => valueManager.ValueCount;

        public DynamicVariable(DynamicVariableSpace.ValueManager<T> manager) : base(manager.Name)
        {
            valueManager = manager;
        }

        protected override object getValue() => Value;

        protected override void setValue(object value) => Value = (T)value;
    }
}