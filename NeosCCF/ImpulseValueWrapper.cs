using FrooxEngine.LogiX.ProgramFlow;
using System;

namespace NeosCCF
{
    internal sealed class ImpulseValueWrapper<T> : FunctionWrapper<T>
    {
        public Func<T, T> Function { get; }

        public ImpulseValueWrapper(Func<T, T> function)
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