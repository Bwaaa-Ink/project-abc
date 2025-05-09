using System;
using System.Linq;
using System.Reflection;
using Mono.Cecil;
using Mono.Cecil.Cil;
using MethodAttributes = Mono.Cecil.MethodAttributes;

namespace TrixxInjection.Fody
{
    internal static class Weaving
    {
        internal static ILProcessor ILP = null;

        public static void Weave()
        {
            var Weaver = ModuleWeaver.That;
            var Module = Weaver.ModuleDefinition;
            var L = Weaver.L;
            InjectStartupModule();
            foreach (var type in Module.Types)
            {
                L.W($"Processing Type {type.FullName}");
                var attributes = type.CustomAttributes;
                using (ILP = new ILProcessor(type))
                {
                    foreach (var attribute in attributes)
                    {
                        L.W($"Processing {type.Name} @ {attribute.AttributeType.FullName}");
                        type.ProcessAttribute(attribute);
                    }
                }
            }
        }

        public static void InjectStartupModule()
        {
            var W = ModuleWeaver.That;
            var configureMethod =
                W.TrixxInjection_Framework_ExpressionTree[AssemblyTypeMethodTree.FileHandling + ".StaticFileHandler",
                    "Configure"].M;
            
            var moduleType = W.ModuleDefinition.Types.Single(t => t.Name == "<Module>");
            var cctor = moduleType.Methods.FirstOrDefault(m => m.Name == ".cctor");
            if (cctor == null)
            {
                cctor = new MethodDefinition(
                    ".cctor",
                    MethodAttributes.Private
                    | MethodAttributes.Static
                    | MethodAttributes.HideBySig
                    | MethodAttributes.SpecialName
                    | MethodAttributes.RTSpecialName, ModuleWeaver.That.ModuleDefinition.TypeSystem.Void);
                moduleType.Methods.Add(cctor);
                var il = cctor.Body.GetILProcessor();
                il.Append(il.Create(OpCodes.Ret));
            }

            var processor = cctor.Body.GetILProcessor();
            var first = cctor.Body.Instructions.First();
            processor.InsertBefore(first, processor.Create(OpCodes.Ldstr, ModuleWeaver.That.Configuration.LogFileName));
            processor.InsertBefore(first, processor.Create(OpCodes.Call, configureMethod));
        }
    }

    internal class ILProcessor : IDisposable
    {
        private static L L => ModuleWeaver.That.L;
        private const string Namespace = "TrixxInjection.Attributes";
        private readonly TypeDefinition Type;

        internal ILProcessor(TypeDefinition type)
        {
            Type = type;
        }

        public void Dispose()
        {
            Weaving.ILP = null;
        }

        internal void Process(CustomAttribute attribute)
        {
            if (attribute.AttributeType.Namespace != Namespace)
            {
                L.W("Attribute was not a weaver marker.");
                return;
            }

            L.W($"Getting Processor for {attribute.AttributeType.Name}");
            var processor = AttributeProcessors.GetProcessor(attribute.AttributeType.Name);
            if (processor == null)
            {
                L.W($"{attribute.AttributeType.Name} has no processor.");
                return;
            }

            try
            {
                processor(attribute, Type);
            }
            catch (Exception ex)
            {
                L.FW($"Processing {attribute.AttributeType.Name} on {Type.FullName} threw a(n) {ex.GetType().FullName}: {ex.Message ?? "No message attached."}");
            }
        }
    }

    internal static class Helpers
    {
        public static void ProcessAttribute(this TypeDefinition type, CustomAttribute attribute)
        {
            Weaving.ILP.Process(attribute);
        }
    }
}
