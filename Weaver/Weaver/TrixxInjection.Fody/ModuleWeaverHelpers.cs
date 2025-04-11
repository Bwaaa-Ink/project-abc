using Fody;
using Mono.Cecil;
using Mono.Cecil.Cil;

namespace TrixxInjection.Fody
{
    internal static class ModuleWeaverHelpers
    {
        public static void ValidateNamespace(string value)
        {
            if (value is null || string.IsNullOrWhiteSpace(value))
            {
                throw new WeavingException("Invalid namespace");
            }
        }
    }
}
