using FrooxEngine.LogiX.ProgramFlow;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CustomEntityFramework.Functions
{
    public abstract class FunctionWrapper<T>
    {
        public Delegate Delegate { get; }

        public FunctionWrapper(Delegate @delegate)
        {
            Delegate = @delegate;
        }

        public abstract bool Invoke(DynamicImpulseTriggerWithValue<T> dynImpulseTrigger, out T result);
    }
}