using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Reflection;
using Mono.Cecil;
using TrixxInjection.Framework.Config;

namespace TrixxInjection.Fody
{
    internal static partial class AttributeProcessors
    {
        private static readonly Dictionary<string, MethodInfo> MethodTree
            = new Dictionary<string, MethodInfo>(StringComparer.Ordinal);

        static AttributeProcessors()
        {
            var methods = typeof(AttributeProcessors)
                .GetMethods(BindingFlags.Static | BindingFlags.NonPublic)
                .Where(mi =>
                {
                    if (mi.ReturnType != typeof(void))
                        return false;

                    var ps = mi.GetParameters();
                    if (ps.Length != 2)
                        return false;

                    return ps[0].ParameterType == typeof(CustomAttribute)
                           && ps[1].ParameterType == typeof(EntityUnion);
                });

            foreach (var mi in methods)
            {
                MethodTree[mi.Name] = mi;
            }
        }

        public static MethodInfo GetProcessor(string attributeName)
        {
            if (MethodTree.TryGetValue(attributeName, out var mi))
                return mi;
            return null;
        }

        public static bool TryProcess(string attributeName, CustomAttribute ca, EntityUnion td)
        {
            if (!MethodTree.TryGetValue(attributeName, out var mi))
                return false;

            mi.Invoke(null, new object[] { ca, td });
            return true;
        }

        private static void AttributeBreak(Enums.AttributeBreaking flag)
        {
            if (!ModuleWeaver.That.Configuration.DEV__AttributesToBreakTo.HasFlag(flag))
                return;
            Debugger.Launch();
            Debugger.Break();
        }

        internal class EntityUnion
        {
            private readonly MethodDefinition _method = null;
            private readonly TypeDefinition _type = null;
            private readonly PropertyDefinition _prop = null;
            private readonly EventDefinition _event = null;
            private readonly FieldDefinition _field = null;
            private readonly AssemblyDefinition _assembly = null;
            private readonly ParameterDefinition _param = null;

            public static implicit operator MethodDefinition(EntityUnion tmu) => tmu._method;
            public static implicit operator TypeDefinition(EntityUnion tmu) => tmu._type;
            public static implicit operator PropertyDefinition(EntityUnion au) => au._prop;
            public static implicit operator EventDefinition(EntityUnion au) => au._event;
            public static implicit operator FieldDefinition(EntityUnion au) => au._field;
            public static implicit operator AssemblyDefinition(EntityUnion au) => au._assembly;
            public static implicit operator ParameterDefinition(EntityUnion au) => au._param;

            public EntityUnion(PropertyDefinition prop) => _prop = prop;
            public EntityUnion(EventDefinition evt) => _event = evt;
            public EntityUnion(FieldDefinition field) => _field = field;
            public EntityUnion(AssemblyDefinition asm) => _assembly = asm;
            public EntityUnion(ParameterDefinition prm) => _param = prm;
            public EntityUnion(TypeDefinition type) => _type = type;
            public EntityUnion(MethodDefinition method) => _method = method;

            public static implicit operator EntityUnion(MethodDefinition md) => new EntityUnion(md);
            public static implicit operator EntityUnion(TypeDefinition td) => new EntityUnion(td);
            public static implicit operator EntityUnion(PropertyDefinition pd) => new EntityUnion(pd);
            public static implicit operator EntityUnion(EventDefinition ed) => new EntityUnion(ed);
            public static implicit operator EntityUnion(FieldDefinition fd) => new EntityUnion(fd);
            public static implicit operator EntityUnion(AssemblyDefinition ad) => new EntityUnion(ad);
            public static implicit operator EntityUnion(ParameterDefinition prm) => new EntityUnion(prm);

            public PropertyDefinition Prop => this;
            public EventDefinition Event => this;
            public FieldDefinition Field => this;
            public AssemblyDefinition Assembly => this;
            public ParameterDefinition Param => this;
            public TypeDefinition Type => this;
            public MethodDefinition Method => this;

            public string FullName => Prop?.FullName ??
                                      Event?.FullName ??
                                      Field?.FullName ??
                                      Assembly?.FullName ??
                                      Param?.Name ??
                                      Type?.FullName ??
                                      Method?.FullName ??
                                      "Union has no name";

        }
    }
}
