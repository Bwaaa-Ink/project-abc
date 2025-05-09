using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Reflection;
using System.Text;
using Mono.Cecil;
using Mono.Cecil.Cil;
using TrixxInjection.FileHandling;

namespace TrixxInjection.Fody
{
    internal partial class AttributeProcessors
    {
        static partial void Serialised(CustomAttribute attribute, TypeDefinition type)
        {
            using (var writer = FileH.WriterFor(ModuleWeaver.That.Configuration.LogFileName))
            {
                L.W($"Explicitly Serialising {type.FullName}");
                writer.Write("Serialising Object by Tag");
                writer.WriteNoTime(new SourceSerialiser().Serialise(type, ModuleWeaver.That.SSC));
                writer.Write("Finished Serialising Object by Tag");
            }
        }

        static partial void Ignored(CustomAttribute attribute, TypeDefinition type)
        {
            throw new NotImplementedException();
        }

        static partial void Timed(CustomAttribute attribute, TypeDefinition type)
        {
            
        }

        static partial void Creation(CustomAttribute attribute, TypeDefinition type)
        {
            type.EnsureDefaultConstructor();
            type.Methods.Where(m => m.IsConstructor).ToList().ForEach(m => StaticFileHandler.AddLog(m.Body, m.IsStatic ? $"INITIALISED [{type.FullName}]" : $"CREATED [{type.FullName}]"));
        }

        static partial void Deletion(CustomAttribute attribute, TypeDefinition type)
        {
            StaticFileHandler.AddLog(type.EnsureDefaultDestructor().Body, $"DESTROYED [{type.FullName}]");
        }

        static partial void MethodDetails(CustomAttribute attribute, TypeDefinition type)
        {

        }

        static partial void Traced(CustomAttribute attribute, TypeDefinition type)
        {

        }
    }
}
