using FrooxEngine.LogiX.ProgramFlow;
using System;

namespace CustomEntityFramework.Functions
{
    internal sealed class DynamicImpulseTriggerValueWrapper<T> : FunctionWrapper<T>
    {
        public new Func<T, T> Delegate { get; }

        public DynamicImpulseTriggerValueWrapper(Func<T, T> function)
            : base(function)
        {
            Delegate = function;
        }

        public override bool Invoke(DynamicImpulseTriggerWithValue<T> dynImpulseTrigger, out T result)
        {
            result = Delegate(dynImpulseTrigger.Value.Evaluate());
            return true;
        }
    }
}