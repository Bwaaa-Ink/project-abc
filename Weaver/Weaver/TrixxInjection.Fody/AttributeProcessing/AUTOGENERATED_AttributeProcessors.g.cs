using Mono.Cecil;
namespace TrixxInjection.Fody
{
    internal static partial class AttributeProcessors
    {
        static partial void Serialised(CustomAttribute attribute, EntityUnion type);
        static partial void Ignored(CustomAttribute attribute, EntityUnion type);
        static partial void Timed(CustomAttribute attribute, EntityUnion type);
        static partial void Creation(CustomAttribute attribute, EntityUnion type);
        static partial void Deletion(CustomAttribute attribute, EntityUnion type);
        static partial void MethodDetails(CustomAttribute attribute, EntityUnion type);
        static partial void Traced(CustomAttribute attribute, EntityUnion type);

        public static void Serialised_Exposer(CustomAttribute attribute, EntityUnion type) => Serialised(attribute, type);
        public static void Ignored_Exposer(CustomAttribute attribute, EntityUnion type) => Ignored(attribute, type);
        public static void Timed_Exposer(CustomAttribute attribute, EntityUnion type) => Timed(attribute, type);
        public static void Creation_Exposer(CustomAttribute attribute, EntityUnion type) => Creation(attribute, type);
        public static void Deletion_Exposer(CustomAttribute attribute, EntityUnion type) => Deletion(attribute, type);
        public static void MethodDetails_Exposer(CustomAttribute attribute, EntityUnion type) => MethodDetails(attribute, type);
        public static void Traced_Exposer(CustomAttribute attribute, EntityUnion type) => Traced(attribute, type);
    }
}
