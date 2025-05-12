using Mono.Cecil;
namespace TrixxInjection.Fody
{
    internal static partial class AttributeProcessors
    {
        static partial void Serialised(CustomAttribute attribute, TypeDefinition type);
        static partial void Ignored(CustomAttribute attribute, TypeDefinition type);
        static partial void Timed(CustomAttribute attribute, TypeDefinition type);
        static partial void Creation(CustomAttribute attribute, TypeDefinition type);
        static partial void Deletion(CustomAttribute attribute, TypeDefinition type);
        static partial void MethodDetails(CustomAttribute attribute, TypeDefinition type);
        static partial void Traced(CustomAttribute attribute, TypeDefinition type);
    }
}
