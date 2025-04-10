using System.Numerics;

namespace PlatformInvokationWrappings
{
    internal static class Attributes
    {
        [AttributeUsage(AttributeTargets.Method, AllowMultiple = false, Inherited = true)]
        internal class RequiresAdministrator : Attribute;
    }

    internal static class Extensions
    {
        public static double D<T>(this T n) where T : struct, INumber<T>
            => double.CreateChecked(n);

        public static bool AlmostEqualTo(this double value1, double value2)
            => Math.Abs(value1 - value2) < 0.000001;
    }
}
