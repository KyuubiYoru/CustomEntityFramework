using HarmonyLib;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;

namespace CustomEntityFramework
{
    public readonly struct DynamicVariableIdentity : IEquatable<DynamicVariableIdentity>
    {
        public readonly string Name;
        public readonly Type Type;

        public DynamicVariableIdentity(Type type, string name)
        {
            Type = type;
            Name = name;
        }

        public DynamicVariableIdentity(object other)
        {
            var traverse = Traverse.Create(other);

            Type = traverse.Field<Type>("type").Value;
            Name = traverse.Field<string>("name").Value;
        }

        public static bool operator !=(DynamicVariableIdentity left, DynamicVariableIdentity right)
        {
            return !(left == right);
        }

        public static bool operator ==(DynamicVariableIdentity left, DynamicVariableIdentity right)
        {
            return left.Equals(right);
        }

        public bool Equals(DynamicVariableIdentity other)
        {
            if (Type == other.Type)
            {
                return Name == other.Name;
            }
            return false;
        }

        public override bool Equals(object obj)
        {
            if (obj is DynamicVariableIdentity other)
            {
                return Equals(other);
            }
            return false;
        }

        public override int GetHashCode()
        {
            return unchecked(-1890651077 * -1521134295 + EqualityComparer<Type>.Default.GetHashCode(Type)) * -1521134295 + EqualityComparer<string>.Default.GetHashCode(Name);
        }
    }
}