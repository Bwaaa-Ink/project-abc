using System;
// ReSharper disable UnusedMember.Global
namespace TrixxInjection.Attributes
{
    /// <summary>
    /// Add this to an object for it to be exclusively serialised.
    /// </summary>
    public class Serialised : Attribute { };

    /// <summary>
    /// Add this to an object for it to be excluded from serialising.
    /// </summary>
    public class Ignored : Attribute { };

    /// <summary>
    /// Times the execution time of the method.
    /// </summary>
    [AttributeUsage(AttributeTargets.Method | AttributeTargets.Constructor)]
    public class Timed : Attribute { };

    /// <summary>
    /// Logs when an instance of the class is created
    /// </summary>
    [AttributeUsage(AttributeTargets.Class)]
    public class Creation : Attribute { };

    /// <summary>
    /// Logs when an instance's destructor is called
    /// </summary>
    [AttributeUsage(AttributeTargets.Class)]
    public class Deletion : Attribute { };

    /// <summary>
    /// Logs things such as Caller, parameters, and more.
    /// </summary>
    [AttributeUsage(AttributeTargets.Method)]
    public class MethodDetails : Attribute { };

    /// <summary>
    /// Logs a full call stack for whatever called this method.
    /// </summary>
    [AttributeUsage(AttributeTargets.Method | AttributeTargets.Property)]
    public class Traced : Attribute { };
}
