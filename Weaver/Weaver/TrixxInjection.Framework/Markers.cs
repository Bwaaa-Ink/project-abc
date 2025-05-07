using System;
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
    public class Timed : Attribute { };

    /// <summary>
    /// Logs when an instance of the class is created
    /// </summary>
    public class Creation : Attribute { };

    /// <summary>
    /// Logs when an instance's destructor is called
    /// </summary>
    public class Deletion : Attribute { };

    /// <summary>
    /// Logs things such as Caller, parameters, and more.
    /// </summary>
    public class MethodDetails : Attribute { };

    /// <summary>
    /// Logs a full call stack for whatever called this method.
    /// </summary>
    public class Trace : Attribute { };
}
