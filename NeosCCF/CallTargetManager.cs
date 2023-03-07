using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Reflection;
using FrooxEngine.LogiX.ProgramFlow;
using NeosModLoader;

namespace NeosCCF
{
    public class CallTargetManager
    {
        public delegate void CallBackDelegate<T>(DynamicImpulseTriggerWithValue<T> value, string[] args);
        
        private Dictionary<Type,Dictionary<string, Delegate>> _callTargets = new Dictionary<Type, Dictionary<string, Delegate>>();

        public void RegisterCallTarget(Type type, string name, Delegate action)
        {
            if (!_callTargets.ContainsKey(type))
            {
                _callTargets.Add(type, new Dictionary<string, Delegate>());
            }

            if (_callTargets[type].ContainsKey(name))
            {
                NeosMod.Debug("CallTarget already exists: " + name);
                return;
            }
            _callTargets[type].Add(name, action);
        }

        protected internal void InvokeCallTarget<T>(string targetString, DynamicImpulseTriggerWithValue<T> value)
        {
            Type type = value.GetType();
            NeosMod.Debug(type);
            if (ParseTargetString(targetString, out string name, out string[] args))
            {
                NeosMod.Debug("Invoking CallTarget: " + name + " with args: " + string.Join(", ", args) + "");
                if (_callTargets.ContainsKey(type))
                {
                    NeosMod.Debug("CallTarget Type found: " + type);
                    if (_callTargets[type].ContainsKey(name))
                    {
                        NeosMod.Debug("CallTarget found: " + name);
                        //_callTargets[typeof(T)][name].Method.Invoke(value, args);
                        //check for null's
                        var method = _callTargets[type][name];
                        if (method == null)
                        {
                            NeosMod.Debug("CallTarget delegate is null: " + name);
                            return;
                        }  
                        
                        if (value == null)
                        {
                            NeosMod.Debug("CallTarget value is null: " + name);
                            return;
                        }
                        
                        NeosMod.Debug("Calling CallTarget: " + name + " with args: " + string.Join(", ", args) + "");
                        method.DynamicInvoke(value, args);
                        NeosMod.Debug("Called CallTarget: " + name + " with args: " + string.Join(", ", args) + "");

                    }else NeosMod.Debug("CallTarget not found: " + name);
                }
                else
                {
                    NeosMod.Debug("CallTarget Type not found: " + type);
                    
                }

            }
        }

        public bool ParseTargetString(string targetString, out string targetName, out string[] args)
        {
            string invalidMsg = "Invalid targetString: " + targetString;

            var targetStringParts = targetString.Split(':');
            if (targetStringParts.Length == 1)
            {
                targetStringParts = targetString.Split('.');
                if (targetStringParts.Length <= 1)
                {
                    targetName = "";
                    args = new string[0];
                    NeosMod.Debug(invalidMsg);
                    return false;
                }

                targetName = string.Join(".", targetStringParts.Take(2));
                args = targetStringParts.Skip(2).ToArray();
                return true;
            }

            if (targetStringParts.Length == 2)
            {
                targetStringParts = targetStringParts[1].Split('.');
                if (targetStringParts.Length <= 1)
                {
                    targetName = "";
                    args = new string[0];
                    NeosMod.Debug(invalidMsg);
                    return false;
                }

                targetName = string.Join(".", targetStringParts.Take(2));
                args = targetStringParts.Skip(2).ToArray();
                return true;
            }

            targetName = "";
            args = new string[0];
            NeosMod.Debug(invalidMsg);
            return false;
        }
    }
}
