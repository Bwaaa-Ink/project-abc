using System.CodeDom;

namespace Weaver_Diagnostic_Viewer
{
#pragma warning disable CS8618
    internal static class MetadataStructs
    {
        internal class Assembly
        {
            public string Name { get; init; }
            public string Version { get; init; }
            public string Culture { get; init; }
            public string PublicTokenKey { get; init; }
            public MethodReference EntryPoint { get; init; }
            public string ModulePath { get; init; }
            public bool IsPrimary { get; init; } = false;
            public uint AssemblyNumber { get; init; }
        }

        internal class Type
        {
            public uint TypeNumber { get; init; }
            public string FullName { get; init; }
            public string SingleName { get; init; }
            public TypeReference BaseType { get; init; }
            public string TypeFlags { get; init; }
            public bool IsInterface { get; set; } = false;
            public bool IsEnum { get; set; } = false;
            public bool IsPrimitive { get; set; } = true;
            public List<Method> Methods { get; init; }
            public List<Type> Interfaces { get; init; }
            public List<Type> NestedTypes { get; init; }
            public List<Field> Fields { get; init; }
            public List<Property> Properties { get; init; }
            public List<Event> Events { get; init; }
            public List<Type> Attributes { get; init; }
            public List<GenericParameter> Generics { get; init; }
        }

        internal enum TypeFlags
        {
            None = 0,
            
        }

        internal class GenericParameter
        {
            public string Name { get; init; }
        }

        internal class Event
        {

        }

        internal class Method
        {
            public string Name { get; init; }
            public uint Number { get; init; }
            public string Attributes { get; init; }
            public uint RVA { get; init; }
            public uint MaxStack { get; init; }
            public uint Token { get; init; }
            public uint InstructionCount { get; init; }
            public List<ExceptionHandler> ExceptionHandlers { get; init; }
            public List<Variable> Variables { get; init; }
            public bool HasBody { get; init; }
            public TypeReference EnclosingType { get; init; }
            public string PInvokeData { get; init; }
            public List<GenericParameter> Generics { get; init; }
            public bool HasOverrides { get; init; }

        }

        public class Variable
        {

        }

        internal class ExceptionHandler
        {

        }

        internal class Field
        {
            public uint Number { get; init; }
            public string Name { get; set; }
            public TypeReference Type { get; set; }
            public string Attributes { get; set; }
            public int Offset { get; set; }
            public uint RVA { get; set; }
            public string InitialValue { get; set; }
            public bool IsConstant { get; set; }

        }

        internal class Property
        {

        }

        internal class MethodReference
        {
            public string Path { get; init; }
            public string Returns { get; init; }
            public string[] Args { get; init; }
            public string Name { get; init; }
        }

        internal class TypeReference
        {
            public string FullName { get; init; }
        }
    }
}
