using System;
using System.Linq;
using System.Reflection;
using Mono.Cecil;
using Mono.Cecil.Cil;
using TrixxInjection.Framework.Config;
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

            var importedConfigureRef = W.Import(configureMethod);

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
            }


            var processor = (Processor)cctor.Body.GetILProcessor();
            var first = cctor.Body.Instructions.First();
            processor.MoveTo(first, false);

            if (W.Configuration.GeneralBehaviour.HasFlag(Enums.GeneralBehaviours.InjectDebugger))
            {
                processor
                    .CallStatic(typeof(System.Diagnostics.Debugger).GetMethodRef("Launch"))
                    .Pop();
            }

            processor
                .PushString(W.Configuration.LogFileName)
                .CallStatic(importedConfigureRef)
                .Return();

            cctor.Body.InitLocals = false;
            cctor.Body.MaxStackSize = 1;
        }
    }

    internal class ILProcessor : IDisposable
    {
        private static L L => ModuleWeaver.That.L;
        private const string Namespace = "TrixxInjection.Framework.Attributes";
        private readonly AttributeProcessors.EntityUnion Entity;

        internal ILProcessor(AttributeProcessors.EntityUnion entity)
        {
            Entity = entity;
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
            try
            {
                if (!AttributeProcessors.TryProcess(attribute.AttributeType.Name, attribute, Entity))
                {
                    L.FW($"{attribute.AttributeType.Name} had no custom implementation");
                }
            }
            catch (Exception ex)
            {
                L.FW($"Processing {attribute.AttributeType.Name} on {Entity.FullName} threw a(n) {ex.GetType().FullName}: {ex.Message ?? "No message attached."}");
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
