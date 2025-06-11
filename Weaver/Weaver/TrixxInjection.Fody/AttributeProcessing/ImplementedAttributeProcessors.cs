using System;
using System.Linq;
using Mono.Cecil;
using TrixxInjection.Framework.Config;

namespace TrixxInjection.Fody
{
    internal partial class AttributeProcessors
    {
        static partial void Serialised(CustomAttribute attribute, EntityUnion type)
        {
            AttributeBreak(Enums.AttributeBreaking.Serialised);
        }

        static partial void Ignored(CustomAttribute attribute, EntityUnion type)
        {
            throw new NotImplementedException();
        }

        static partial void Timed(CustomAttribute attribute, EntityUnion type)
        {
            AttributeBreak(Enums.AttributeBreaking.Timed);

        }

        static partial void Creation(CustomAttribute attribute, EntityUnion attr)
        {
            AttributeBreak(Enums.AttributeBreaking.Creation);
            var type = attr.Type;
            type.EnsureDefaultConstructor();
            type.Methods.Where(m => m.IsConstructor).ToList().ForEach(m => m.Body.AddLog(m.IsStatic ? $"INITIALISED [{type.FullName}]" : $"CREATED [{type.FullName}]"));
        }

        static partial void Deletion(CustomAttribute attribute, EntityUnion attr)
        {
            AttributeBreak(Enums.AttributeBreaking.Deletion);
            var type = attr.Type;
            type.EnsureDefaultDestructor().Body.AddLog($"DESTROYED [{type.FullName}]");
        }

        static partial void MethodDetails(CustomAttribute attribute, EntityUnion type)
        {
            AttributeBreak(Enums.AttributeBreaking.MethodDetails);
        }

        static partial void Traced(CustomAttribute attribute, EntityUnion type)
        {
            AttributeBreak(Enums.AttributeBreaking.Traced);
        }
    }
}
