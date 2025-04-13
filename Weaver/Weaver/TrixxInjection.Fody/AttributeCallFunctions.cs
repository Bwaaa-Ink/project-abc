using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Mono.Cecil;
using Mono.Cecil.Cil;

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
                ._(type.HasMethods ? Leths(type.Methods.ToArray()) : "No Methods.")
                ._(type.HasCustomAttributes ? string.Join(", ", type.CustomAttributes.Select(a => a.AttributeType.FullName)) : "Has no Custom Attributes")
                ._(type.HasGenericParameters ? $"{_indent} Generics:\n{_indent} " + string.Join($"\n{_indent}  ", type.GenericParameters.Select(gp => $"Unimplemented (was lazy)")) : "Method has no Generic Parameters.")
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

            sb._($"Method: {meth.Name}");
            IndentLevel++;
            sb._($"Method Number: {meth.DeclaringType.Methods.IndexOf(meth.DeclaringType.Methods.First(m => m.FullName == meth.FullName))}")
            ._("Method Attributes, Implementation Attributes and Semantic attributes: " + string.Join(", ", 
                Extensions.GetFlagNames<MethodAttributes>((ushort)meth.Attributes).Concat(
                    Extensions.GetFlagNames<MethodImplAttributes>((ushort)meth.ImplAttributes)
                    ).Concat(
                    Extensions.GetFlagNames<MethodSemanticsAttributes>((ushort)meth.SemanticsAttributes)
                    )
                ))
            ._(meth.HasCustomAttributes ? string.Join(", ", meth.CustomAttributes.Select(a => a.AttributeType.FullName)) : "Has no Custom Attributes")
            ._($"RVA: {meth.RVA}")
            ._(meth.HasBody ? $" Max Stack Size: {meth.Body.MaxStackSize}\n" +
                              $"{_indent} Token: {meth.Body.LocalVarToken.ToUInt32()}\n" +
                              $"{_indent} Max Code Size: {meth.Body.CodeSize}\n" +
                              $"{_indent} ({(meth.Body.HasExceptionHandlers ? string.Join("\n{_indent} ", meth.Body.ExceptionHandlers.Select(eh => $" Catching: {eh.CatchType.FullName}\n{_indent}  Type: {eh.HandlerType}")) : $"Body contains no Exception Handling.")})\n" +
                              (meth.Body.HasVariables ? $"{_indent} Variables\n{_indent}  " + string.Join($"\n{_indent}  ", meth.Body.Variables.Select(v => $"{v.VariableType.FullName}, #{v.Index}")) : $"{_indent} Contains no variables.")
                : $"{_indent} Method has no body.")
            ._(meth.HasPInvokeInfo ? $"PInvoke Data:\n{_indent} Containing Module: {meth.PInvokeInfo.Module.Name}\n{_indent} Entry Point: {meth.PInvokeInfo.EntryPoint}\n{_indent} Tags: {string.Join(", ", Extensions.GetFlagNames<PInvokeAttributes>(meth.PInvokeInfo.Attributes))}" : "Method has no PInvoke data.")
            ._(meth.HasGenericParameters ? $"{_indent} Generics:\n{_indent} " + string.Join($"\n{_indent}  ", meth.GenericParameters.Select(gp => $"Unimplemented (was lazy)")) : "Method has no Generic Parameters.")
            ._($"Has Overrides: {meth.HasOverrides}");
            IndentLevel--;
            return sb;
        }

        public static string Leths(MethodDefinition[] meths, bool includeType = true)
        {
            IndentLevel++;
            var sb = new StringBuilder();
            foreach (var meth in meths)
            {
                sb.Leth(meth, includeType);
            }
            IndentLevel--;
            return sb.ToString();
        }

        public static string Latts()
        {

        }

        public static string Latt()
        {

        }
    }
}
