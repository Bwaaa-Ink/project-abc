namespace PlatformInvokationWrappings
{
    internal static class Attributes
    {
        [AttributeUsage(AttributeTargets.Method, AllowMultiple = false, Inherited = true)]
        internal class RequiresAdministrator : Attribute;
    }
}
