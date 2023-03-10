using FrooxEngine;
using HarmonyLib;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CustomEntityFramework
{
    public static class DynamicVariableSpaceHelper
    {
        public static DynamicVariable<T> GetVariable<T>(this DynamicVariableSpace space, string name)
        {
            return space.GetVariables<T>().First(variable => variable.Name == name);
        }

        public static DynamicVariable GetVariable(this DynamicVariableSpace space, string name)
        {
            return space.GetVariables(name).Single();
        }

        public static IEnumerable<DynamicVariable> GetVariables(this DynamicVariableSpace space, string name)
        {
            return space.GetVariables(variable => variable.Name == name);
        }

        public static IEnumerable<DynamicVariable<T>> GetVariables<T>(this DynamicVariableSpace space)
        {
            var type = typeof(T);

            return space.GetVariables(variable => variable.Type == type).Cast<DynamicVariable<T>>();
        }

        public static IEnumerable<DynamicVariable> GetVariables(this DynamicVariableSpace space, Func<DynamicVariable, bool> predicate)
        {
            return space.GetVariables().Where(predicate);
        }

        public static IEnumerable<DynamicVariable> GetVariables(this DynamicVariableSpace space)
        {
            return space._dynamicValues.Select(variable => DynamicVariable.Create(variable.Key, variable.Value));
        }
    }
}