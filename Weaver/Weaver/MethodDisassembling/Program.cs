using System.Diagnostics;

namespace MethodDisassembling
{
    internal class Program
    {
        private static Class @class;

        static void Main(string[] args)
        {
            Console.WriteLine("Hello, World!");
            @class = new();
            @class.RunStopWatcher();
        }

        private static void M2()
        {
            @class.RunStopWatcher();
    }

        public class Class
        {
            public Class()
            {
                _ = "yo";
                M2();
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
    }
}
