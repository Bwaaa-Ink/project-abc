using System;
namespace TrixxInjection
{
    /// <summary>
    /// Add this to an object for it to be exclusively serialised.
    /// </summary>
    public class Serialised : Attribute { };

    /// <summary>
    /// Add this to an object for it to be excluded from serialising.
    /// </summary>
    public class Ignored : Attribute { };
}
