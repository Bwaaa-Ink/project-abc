using System;
using System.Collections.Generic;
using System.Data.Common;
using System.Linq;
using System.Text;
using Mono.Cecil;

namespace TrixxInjection.Fody
{
    internal class AssemblyTypeMethodTree
    {
        public const string TI = "TrixxInjection";
        public static string FileHandling => TI + "TrixxInjection.FileHandling";
        private readonly Dictionary<string, (TypeDefinition, Dictionary<string, List<MethodDefinition>>)> _tree;

        public AssemblyTypeMethodTree(AssemblyDefinition asm)
        {
            _tree = asm.Modules
                .SelectMany(m => m.Types)
                .ToDictionary(
                    t => t.FullName,
                    t => (
                        t,
                        t.Methods
                            .GroupBy(m => m.Name)
                            .ToDictionary(g => g.Key, g => g.ToList())
                    )
                );
        }

        public TypeMethodUnion this[string type, string method = "", Func<MethodDefinition, bool> func = null] =>
            _tree.TryGetValue(type, out var expr)
                ? (!string.IsNullOrWhiteSpace(method)
                    ? (expr.Item2.TryGetValue(method, out var _methods) ? (TypeMethodUnion)(_methods.First(func ?? (d => true))) : null)
                    : (TypeMethodUnion)expr.Item1)
                : null;
    }


    public class TypeMethodUnion
    {
        private readonly MethodDefinition _method = null;
        private readonly TypeDefinition _type = null;

        public TypeMethodUnion(TypeDefinition type)
        {
            _type = type;
        }

        public TypeMethodUnion(MethodDefinition method)
        {
            _method = method;
        }

        public static implicit operator MethodDefinition(TypeMethodUnion tmu)
            => tmu._method;

        public static implicit operator TypeDefinition(TypeMethodUnion tmu)
            => tmu._type;

        public static implicit operator TypeMethodUnion(MethodDefinition md)
            => new TypeMethodUnion(md);

        public static implicit operator TypeMethodUnion(TypeDefinition td)
            => new TypeMethodUnion(td);

        public TypeDefinition T => this;
        public MethodDefinition M => this;
    }
}
