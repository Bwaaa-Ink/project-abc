using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Reflection;
using System.Text;
using Mono.Cecil;
using Mono.Cecil.Cil;
using TrixxInjection.Framework.Config;
using TrixxInjection.Framework.FileHandling;
using This = TrixxInjection.Fody.ModuleWeaver;

namespace TrixxInjection.Fody
{
    internal partial class AttributeProcessors
    {
        static partial void Serialised(CustomAttribute attribute, TypeDefinition type)
        {
            AttributeBreak(Enums.AttributeBreaking.Serialised);
        }

        static partial void Ignored(CustomAttribute attribute, TypeDefinition type)
        {
            throw new NotImplementedException();
        }

        static partial void Timed(CustomAttribute attribute, TypeDefinition type)
        {
            AttributeBreak(Enums.AttributeBreaking.Timed);
        }

        static partial void Creation(CustomAttribute attribute, TypeDefinition type)
        {
            AttributeBreak(Enums.AttributeBreaking.Creation);
            type.EnsureDefaultConstructor();
            type.Methods.Where(m => m.IsConstructor).ToList().ForEach(m => m.Body.AddLog(m.IsStatic ? $"INITIALISED [{type.FullName}]" : $"CREATED [{type.FullName}]"));
        }

        static partial void Deletion(CustomAttribute attribute, TypeDefinition type)
        {
            AttributeBreak(Enums.AttributeBreaking.Deletion);
            type.EnsureDefaultDestructor().Body.AddLog($"DESTROYED [{type.FullName}]");
        }

        static partial void MethodDetails(CustomAttribute attribute, TypeDefinition type)
        {
            AttributeBreak(Enums.AttributeBreaking.MethodDetails);
        }

        static partial void Traced(CustomAttribute attribute, TypeDefinition type)
        {
            AttributeBreak(Enums.AttributeBreaking.Traced);
        }
    }
}
