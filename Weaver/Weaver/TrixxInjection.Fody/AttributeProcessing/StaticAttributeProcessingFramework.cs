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
                        && ps[1].ParameterType == typeof(TypeDefinition);
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

        public static bool TryProcess(string attributeName, CustomAttribute ca, TypeDefinition td)
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
    }
}
