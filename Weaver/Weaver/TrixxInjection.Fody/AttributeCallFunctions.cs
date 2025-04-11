using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Mono.Cecil;

namespace TrixxInjection.Fody
{
    internal static class Diagnostics
    {
        internal static int IndentLevel { get => _indent.Length; set => _indent = new string(' ', value); }
        private static string _indent = "";

        public static StringBuilder _(this StringBuilder sb, string message)
        {
            sb.AppendLine(_indent + message);
            return sb;
        }

        public static StringBuilder Lass(this StringBuilder sb, AssemblyDefinition ass)
            => sb._($"Assembly Full Name: {ass.FullName}").
                _($"Entry Point: {ass.EntryPoint}").
                _($"Entry Module name: {ass.MainModule.FileName}");

        public static StringBuilder Lyte(this StringBuilder sb, TypeDefinition type)
            => sb._($"Full Name: {type.FullName}")
                ._($"Single Name: {type.Name}")
                ._(type.HasLayoutInfo ? $"Class Size: {type.ClassSize}\nPacking Size: {type.PackingSize}" : "Failed to resolve layout information.")
                ._($"Base Type Full Name: {type.BaseType.FullName}")
                ._(type.HasInterfaces ? ($"Interfaces: " + string.Join("\n " + _indent, type.Interfaces.Select(i => i.InterfaceType.FullName))) : "No Interfaces")
                ._(type.HasNestedTypes ? $"Nested Types: " + string.Join(("\n " + _indent), type.NestedTypes.Select(n => n.FullName)) : "No Nested Types.")
                ._($"")
                ._($"")
                ._($"")
                ._($"")
                ._($"")
                ._($"")
                ._($"")
                ._($"")
            ;


        public static StringBuilder Leth(this StringBuilder sb, MethodDefinition meth, bool includeType = true)
        {
            if (includeType)
            {
                sb._($"Enclosing Type: {meth.DeclaringType.FullName}");
            }

            sb._($"Method Number: {meth.DeclaringType.Methods.IndexOf(meth.DeclaringType.Methods.First(m => m.FullName == meth.FullName))}");
            sb._($"Inlining: {meth.AggressiveInlining}");
            sb._($"Forced Optimisation: {meth.AggressiveOptimization}");
            sb._($"{meth.}");
            return sb;
        }

        public static string Leths(MethodDefinition[] meths, bool includeType = true)
        {
            var sb = new StringBuilder();
            foreach (var meth in meths)
            {
                sb.Leth(meth, includeType);
            }

            return sb.ToString();
        }
    }
}
