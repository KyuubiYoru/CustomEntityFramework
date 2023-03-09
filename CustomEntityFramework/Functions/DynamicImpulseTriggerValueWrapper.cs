using FrooxEngine.LogiX.ProgramFlow;
using System;

namespace CustomEntityFramework.Functions
{
    internal sealed class DynamicImpulseTriggerValueWrapper<T> : FunctionWrapper<T>
    {
        public Func<T, T> Function { get; }

        public DynamicImpulseTriggerValueWrapper(Func<T, T> function)
            : base(function)
        {
            Function = function;
        }

        public override T Invoke(DynamicImpulseTriggerWithValue<T> dynImpulseTrigger)
        {
            return Function(dynImpulseTrigger.Value.Evaluate());
        }
    }
}