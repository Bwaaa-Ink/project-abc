#define DIAGNOSTICS
//#define ATTACH_DEBUG

using System;
using Fody;
using Mono.Cecil.Rocks;
using Mono.Cecil;
using System.Collections.Generic;
using System.Linq;
using System.IO;
using System.Text;
using Mono.Cecil.Cil;
using static TrixxInjection.Fody.ModuleWeaverHelpers;
using MethodAttributes = Mono.Cecil.MethodAttributes;

namespace TrixxInjection.Fody
{

    public class ModuleWeaver : BaseModuleWeaver
    {
        public override bool ShouldCleanReference => true;

        public ModuleWeaver()
        {
            ModuleWeaverHelpers.Weaver = this;
        }

        public override void Execute()
        {
#if ATTACH_DEBUG
            System.Diagnostics.Debugger.Launch();
#endif
            try
            {
                StringBuilder sb = new StringBuilder();
#if DIAGNOSTICS
                sb._("START OF DIAGNOSTICS");
                WriteInfo("Starting Weaver Diagnostics");
                WriteInfo("Using Automatic JSON Serialisation");
                var a = ModuleDefinition.Assembly;
                sb.AppendLine(
                    SerializeToJson(
                        a,
                        new List<string>()
                        {
                            nameof(Instruction.Next),
                            nameof(Instruction.Previous),
                            nameof(OpCode.FlowControl),
                            nameof(OpCode.OpCodeType),
                            nameof(OpCode.OperandType),
                            nameof(OpCode.StackBehaviourPop),
                            nameof(OpCode.StackBehaviourPush),
                            nameof(Mono.Cecil.ModuleDefinition.TypeSystem),
                            "PublicKey"
                        },
                        new List<string>()
                        {
                            nameof(Mono.Cecil.ModuleDefinition.Assembly),
                        }
                    )
                );
                sb._("END OF DIAGNOSTICS");
#endif
                File.WriteAllText("C:/Logs/Diagnostic_Test.txt", sb.ToString());
            }
            catch (WeavingException wex)
            {
                WriteError(wex.Message);
            }
            catch (InvalidOperationException ex)
            {
                WriteError($"An invalid operation occured: {ex.Message}   @   {ex.Source}   stacked with   {ex.StackTrace}");
            }
            catch (Exception ex)
            {
                WriteError($"A {ex.GetType().Name} occured: {ex.Message}   @   {ex.Source}   stacked with   {ex.StackTrace}");
            }
            finally
            {
                WriteInfo("Weaving Complete.");
            }
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