using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace TrixxInjection.Fody
{
    internal static class Extensions
    {
        public static string[] GetFlagNames<T>(object value) where T : Enum
        {
            var numericValue = Convert.ToUInt64(value);
            return Enum.GetValues(typeof(T)).Cast<T>()
                .Where(flag =>
                {
                    var flagValue = Convert.ToUInt64(flag);
                    return flagValue != 0 && (numericValue & flagValue) == flagValue;
                })
                .Select(flag => flag.ToString())
                .ToArray();
        }
    }
}
