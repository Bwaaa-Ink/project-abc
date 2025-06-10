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

        public static void Serialised_Exposer(CustomAttribute attribute, TypeDefinition type) => Serialised(attribute, type);
        public static void Ignored_Exposer(CustomAttribute attribute, TypeDefinition type) => Ignored(attribute, type);
        public static void Timed_Exposer(CustomAttribute attribute, TypeDefinition type) => Timed(attribute, type);
        public static void Creation_Exposer(CustomAttribute attribute, TypeDefinition type) => Creation(attribute, type);
        public static void Deletion_Exposer(CustomAttribute attribute, TypeDefinition type) => Deletion(attribute, type);
        public static void MethodDetails_Exposer(CustomAttribute attribute, TypeDefinition type) => MethodDetails(attribute, type);
        public static void Traced_Exposer(CustomAttribute attribute, TypeDefinition type) => Traced(attribute, type);
    }
}
