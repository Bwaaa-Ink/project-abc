using Mono.Cecil;
namespace TrixxInjection.Fody
{
    internal static partial class AttributeProcessors
    {
        static partial void Serialised(CustomAttribute attribute, EntityUnion attr);
        static partial void Ignored(CustomAttribute attribute, EntityUnion attr);
        static partial void Timed(CustomAttribute attribute, EntityUnion attr);
        static partial void Creation(CustomAttribute attribute, EntityUnion attr);
        static partial void Deletion(CustomAttribute attribute, EntityUnion attr);
        static partial void MethodDetails(CustomAttribute attribute, EntityUnion attr);
        static partial void Traced(CustomAttribute attribute, EntityUnion attr);

        public static void Serialised_Exposer(CustomAttribute attribute, EntityUnion attr) => Serialised(attribute, attr);
        public static void Ignored_Exposer(CustomAttribute attribute, EntityUnion attr) => Ignored(attribute, attr);
        public static void Timed_Exposer(CustomAttribute attribute, EntityUnion attr) => Timed(attribute, attr);
        public static void Creation_Exposer(CustomAttribute attribute, EntityUnion attr) => Creation(attribute, attr);
        public static void Deletion_Exposer(CustomAttribute attribute, EntityUnion attr) => Deletion(attribute, attr);
        public static void MethodDetails_Exposer(CustomAttribute attribute, EntityUnion attr) => MethodDetails(attribute, attr);
        public static void Traced_Exposer(CustomAttribute attribute, EntityUnion attr) => Traced(attribute, attr);
    }
}
