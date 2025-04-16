using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Mono.Cecil;
using Mono.Cecil.Cil;
using Mono.Cecil.Rocks;

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

        static int count = 0;

        public static StringBuilder Lyte(this StringBuilder sb, TypeDefinition type)
        {
            sb._($"Full Name: {type.FullName}");
            try { sb._($"Single Name: {type.Name}"); } catch (NullReferenceException) { sb._("type.Name was null."); }

            try { sb._(type.HasLayoutInfo ? $"Class Size: {type.ClassSize}\nPacking Size: {type.PackingSize}" : "Failed to resolve layout information."); } catch (NullReferenceException) { sb._("type.HasLayoutInfo or type.ClassSize or type.PackingSize was null."); }

            try { sb._($"Base Type Full Name: {type.BaseType.FullName}"); } catch (NullReferenceException) { sb._("type.BaseType.FullName was null."); }

            try { sb._(type.HasInterfaces ? ("Interfaces: " + string.Join("\n " + _indent, type.Interfaces.Select(i => i.InterfaceType.FullName))) : "No Interfaces"); } catch (NullReferenceException) { sb._("type.Interfaces or i.InterfaceType.FullName was null."); }

            IndentLevel++;
            try { sb._(type.HasNestedTypes ? $"Nested Types: " + string.Join("\n " + _indent, type.NestedTypes.Select(n => sb.Lyte(n))) : "No Nested Types."); } catch (NullReferenceException) { sb._("type.NestedTypes or n.FullName was null."); }

            IndentLevel--;
            try { sb._(type.HasMethods ? Leths(type.Methods.ToArray(), false) : "No Methods."); } catch (NullReferenceException) { sb._("type.Methods was null."); }

            try { sb._(type.HasCustomAttributes ? "Custom Attributes: " + string.Join(", ", type.CustomAttributes.Select(a => a.AttributeType.FullName)) : "Has no Custom Attributes"); } catch (NullReferenceException) { sb._("type.CustomAttributes or a.AttributeType.FullName was null."); }

            try { sb._(type.HasGenericParameters ? $"{_indent} Generics:\n{_indent} " + string.Join($"\n{_indent} ", type.GenericParameters.Select(gp => $"Unimplemented (was lazy)")) : "Method has no Generic Parameters."); } catch (NullReferenceException) { sb._("type.GenericParameters was null."); }

            try { sb._(type.HasFields ? $"Fields: \n{_indent} " + string.Join($"\n{_indent} ", type.Fields.Select(f => { string sep = $"\n{_indent} "; count++; string s = $"Field number {count}{sep}Name: {f.FullName}{sep}Type: {f.FieldType.FullName}{sep}Attributes: {string.Join(", ", Extensions.GetFlagNames<FieldAttributes>((ushort)f.Attributes))}{sep}Offset: {f.Offset}{sep}RVA: {f.RVA}{sep}Initial Value: {string.Join(" ", f.InitialValue)}{sep}{(f.HasConstant ? $"Constant value: {f.Constant}" : "Field is not constant.")}{sep}{(f.HasCustomAttributes ? string.Join(", ", f.CustomAttributes.Select(a => a.AttributeType.FullName)) : "Has no Custom Attributes")}{sep}"; return s; })) : "Type has no fields."); } catch (NullReferenceException) { sb._("type.Fields or one of the field properties was null."); }

            try { sb._(type.HasEvents ? $"Events: \n{_indent} " + string.Join(", ", type.Events.Select(e => e.FullName)) : "Type has no events."); } catch (NullReferenceException) { sb._("type.Events or e.FullName was null."); }

            try { sb._(type.HasProperties ? $"Properties: \n{_indent} " + string.Join("\n{_indent} ", type.Properties.Select(p => { string sep = $"\n{_indent} "; string str = $"Property {p.FullName}{sep}{(p.HasCustomAttributes ? "Custom attributes: " + string.Join(", ", p.CustomAttributes.Select(a => a.AttributeType.FullName)) : "Property has no custom attributes")}{sep}Is Constant? {p.HasConstant}{sep}Is Instance? {p.HasThis}"; return str; })) : "Type has no Properties"); } catch (NullReferenceException) { sb._("type.Properties or one of the property references was null."); }

            try { sb._($"Type Attributes: {string.Join(", ", Extensions.GetFlagNames<TypeAttributes>((uint)type.Attributes))}"); } catch (NullReferenceException) { sb._("type.Attributes was null."); }

            try { sb._($"Is Enum? {type.IsEnum}"); } catch (NullReferenceException) { sb._("type.IsEnum was null."); }

            try { sb._($"Is Value Type? {type.IsValueType}")._($"Is Primitive Type? {type.IsPrimitive}"); } catch (NullReferenceException) { sb._("type.IsValueType or type.IsPrimitive was null."); }
            return sb;
        }


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
    }
}
