using System.ComponentModel;
using System.ComponentModel.Design;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;
using TrixxInjection;

#pragma warning disable

namespace MethodDisassembling
{
    internal class Program
    {
        private static Class @class;

        public bool AProperty { get; set; }
        public const bool aConstant = false;
        public string AGetProperty => "bwaaa";
        public string AnInitProperty { get; init; }
        public string ADefaultProperty { get; set; } = "default";

        public delegate void DoSomething(string AValue);

        public static event DoSomething AnEvent;

        static void Main(string[] args)
        {
            Console.WriteLine("Hello, World!");
            @class = new();
            @class.RunStopWatcher();
            AnEvent += (s) => { Console.WriteLine("Hmmmm"); };
            Console.Read();
        }

        public static void M2()
        {
            try
            {
                try
                {
                    @class.RunStopWatcher();
                }
                catch (NullReferenceException)
                {
                    throw new InvalidOperationException();
                }
            }
            catch (InvalidOperationException)
            {
                Console.Write("hmmm");
            }
            catch (ArgumentException ex) when (ex.HResult >= 1)
            {
                Console.Write("hrrrnnnnnn");
            }
            finally
            {
                Console.Write("Its overrr");
            }
        }

        public readonly struct AStruct()
        {
            public readonly bool AField = true;
            public readonly bool AProperty => AField;

            public readonly void Bwaa()
                => _ = $"Lorem Ipsum + {AField}";
        }

        private static void MethodWithParameters(ref string b, string a, double ba)
        {
            b = a + ba.ToString();
        }
    }

    [Flags]
    public enum Numbers
    {
        One,
        Two,
        Three,
        Four,
        Five,
        Six,
        Seven = 0b_111,
        Eight,
        Nine,
        [An]
        Ten = 99
    }

    [TrixxInjection.Attributes.Creation]
    [TrixxInjection.Attributes.Deletion]
    public class Class
    {
        public Class()
        {
            _ = "yo";
            Program.M2();
        }

        public void RunStopWatcher()
        {
            var stopwatcher = new Stopwatch();
            stopwatcher.Start();
            stopwatcher.Stop();
            var ms = stopwatcher.ElapsedMilliseconds;
            Console.WriteLine($"Stopwatch final time: {ms}");
        }
    }

    [TrixxInjection.Attributes.Creation]
    [TrixxInjection.Attributes.Deletion]
    public class Class2
    {
        public bool field = false;
    }

    [TrixxInjection.Attributes.Creation]
    [TrixxInjection.Attributes.Deletion]
    public class Class3 : Class2
    {
        public new bool field = true;
    }

    public class AnAttribute : Attribute;
}
