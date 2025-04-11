#define DIAGNOSTICS

using Fody;
using Mono.Cecil.Rocks;
using Mono.Cecil;
using System.Collections.Generic;
using System.Linq;
using System.IO;
using System.Reflection;
using System.Text;
using Mono.Cecil.Cil;
using static TrixxInjection.Fody.ModuleWeaverHelpers;
using MethodAttributes = Mono.Cecil.MethodAttributes;
using static TrixxInjection.Fody.Diagnostics;

namespace TrixxInjection.Fody
{

    public class ModuleWeaver : BaseModuleWeaver
    {
        public override bool ShouldCleanReference => true;

        public ModuleWeaver()
        {
            
        }

        public override void Execute()
        {
#if DIAGNOSTICS
            StringBuilder sb = new StringBuilder();
            sb.AppendLine($"START OF DIAGNOSTICS");

            #region ASSEMBLY

            var a = ModuleDefinition.Assembly;
            sb.Lass(a)._("[References Assemblies]");

            int count = 0;
            foreach (var mdar in ModuleDefinition.AssemblyReferences)
            {
                sb._($"ASM: {++count}");
                IndentLevel++;
                sb.Lass(ModuleDefinition.AssemblyResolver.Resolve(mdar));
                IndentLevel--;
            }

            sb._("[END OF REFERENCE ASSEMBLIES]")
                ._("[START OF IMPLEMENTED TYPES]");
            count = 0;
            foreach (var td in ModuleDefinition.Types)
            {
                sb._($"TYPE: {++count}");
                IndentLevel++;
                
                IndentLevel--;
            }


            #endregion

#endif
        }

        public override IEnumerable<string> GetAssembliesForScanning()
        {
            yield return "netstandard";
            yield return "mscorlib";
        }


        private string GetNamespace()
        {
            var namespaceFromConfig = GetNamespaceFromConfig();
            var namespaceFromAttribute = GetNamespaceFromAttribute();
            if (namespaceFromConfig != null && namespaceFromAttribute != null)
            {
                throw new WeavingException("Configuring namespace from both Config and Attribute is not supported.");
            }

            return namespaceFromAttribute ?? namespaceFromConfig;
        }

        private string GetNamespaceFromConfig()
        {
            var attribute = Config?.Attribute("Namespace");
            if (attribute == null)
            {
                return null;
            }

            var value = attribute.Value;
            ValidateNamespace(value);
            return value;
        }

        private void _(TypeDefinition @class)
        {
            for (int i = 0; i < @class.Methods.Count; i++)
                @class.Methods.RemoveAt(0);
        }

        private void AddConstructor(TypeDefinition newType)
        {
            const MethodAttributes attributes = MethodAttributes.Public | MethodAttributes.SpecialName | MethodAttributes.RTSpecialName;
            var method = new MethodDefinition(".ctor", attributes, TypeSystem.VoidReference);
            var objectConstructor = ModuleDefinition.ImportReference(TypeSystem.ObjectDefinition.GetConstructors().First());
            var processor = method.Body.GetILProcessor();
            processor.Emit(OpCodes.Ldarg_0);
            processor.Emit(OpCodes.Call, objectConstructor);
            processor.Emit(OpCodes.Ret);
            newType.Methods.Add(method);
        }

        private string GetNamespaceFromAttribute()
        {
            var attributes = ModuleDefinition.Assembly.CustomAttributes;
            var namespaceAttribute = attributes
                .SingleOrDefault(x => x.AttributeType.FullName == "NamespaceAttribute");
            if (namespaceAttribute == null)
            {
                return null;
            }

            attributes.Remove(namespaceAttribute);
            var value = (string)namespaceAttribute.ConstructorArguments.First().Value;
            ValidateNamespace(value);
            return value;
        }
    }
}