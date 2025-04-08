using System.Drawing;
using PlatformInvokationWrappings;

namespace SB_PlatformInvocationWrappings_Console
{
    internal class Program
    {
        static void Main(string[] args)
        {
            while (true)
            {
                Console.WriteLine("""
                                  rc -n: Replace n with a uint, will read the cursor position that many times.
                                  scr -n: Replace n with a uint, will set the cursor to a random location that many times.
                                  """);
                var @string = Console.ReadLine() ?? "";
                if (@string.StartsWith("rc -", StringComparison.InvariantCultureIgnoreCase))
                {
                    if (!int.TryParse(@string.Split("rc -").Last(), out var result))
                    {
                        Console.WriteLine("Failed to parse uint.");
                        return;
                    }

                    for (var count = 0; count < result; count++)
                        Console.WriteLine(Extended.Mouse.Position);
                }
                else if (@string.StartsWith("scr -", StringComparison.InvariantCultureIgnoreCase))
                {
                    if (!int.TryParse(@string.Split("scr -").Last(), out var result))
                    {
                        Console.WriteLine("Failed to parse uint.");
                        return;
                    }

                    var r = new Random();
                    var bounds = WinWrappers.GetPrimaryWorkspaceWidthHeight();
                    for (var count = 0; count < result; count++)
                    {
                        Extended.Mouse.Position = new Point(r.Next(0, bounds.Item1), r.Next(0, bounds.Item2));
                        Thread.Sleep(5);
                    }
                }
            }
        }
    }
}
