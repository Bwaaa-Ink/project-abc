using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using Mono.Cecil;

namespace TrixxInjection.Fody
{
    internal static partial class AttributeProcessors
    {
        private static L L
            => ModuleWeaver.That.L;
        private static Dictionary<string, Action<CustomAttribute, TypeDefinition>> ActionTree { get; } =
            new Dictionary<string, Action<CustomAttribute, TypeDefinition>>();

        static AttributeProcessors()
        {
            var apt = typeof(Action<CustomAttribute, TypeDefinition>);
            typeof(AttributeProcessors)
                .GetMethods(BindingFlags.Static | BindingFlags.NonPublic)
                .ToList()
                .ForEach(
                    m => ActionTree
                        .Add(
                            m.Name,
                            (Action<CustomAttribute, TypeDefinition>)Delegate.CreateDelegate(
                                apt,
                                m
                            )
                        )
                );
        }

        public static Action<CustomAttribute, TypeDefinition> GetProcessor(string attributeName)
            => ActionTree.TryGetValue(attributeName, out var value) ? value : null;
    }
}
