using System.Drawing;
using PlatformInvokationWrappings;

namespace SB_PlatformInvocationWrappings_Console
{
    internal class Program
    {
        static void Main(string[] args)
        {
            Run();
        }

        static async Task Run()
        {
            bool Jittering = false;
            while (true)
            {
                Console.WriteLine("""
                                  rc -n: Replace n with a uint, will read the cursor position that many times.
                                  scr -n: Replace n with a uint, will set the cursor to a random location that many times.
                                  jitter -n: Jitters the mouse n pixel amount
                                  no jitter: Stops jittering
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
                        Console.WriteLine(WinWrappers.GetMousePos());
                }
                else if (@string.StartsWith("scr -", StringComparison.InvariantCultureIgnoreCase))
                {
                    if (!uint.TryParse(@string.Split("scr -").Last(), out var result))
                    {
                        Console.WriteLine("Failed to parse uint.");
                        return;
                    }

                    var r = new Random();
                    var bounds = WinWrappers.GetPrimaryWorkspaceWidthHeight();
                    for (var count = 0; count < result; count++)
                    {
                        WinWrappers.SetMousePos(r.Next(0, bounds.Item1), r.Next(0, bounds.Item2));
                        Thread.Sleep(5);
                    }
                }
                else if (@string.StartsWith("jitter -", StringComparison.InvariantCultureIgnoreCase))
                {
                    if (Jittering)
                    {
                        Console.WriteLine("Is already jittering");
                        continue;
                    }
                    if (!uint.TryParse(@string.Split("jitter -").Last(), out var result))
                    {
                        Console.WriteLine("Failed to parse uint.");
                        continue;
                    }

                    Jittering = true;
                    Jitter((int)result, ref Jittering);
                }
                else if (@string.StartsWith("no jitter", StringComparison.InvariantCultureIgnoreCase))
                {
                    Jittering = false;
                }
            }
        }

        static void Jitter(int res, ref bool Jittering)
        {
            var rand = new Random();
            while (Jittering)
            {
                Thread.Sleep(5);
                var p = WinWrappers.GetMousePos();
                WinWrappers.SetMousePos(p.X + rand.Next(-res, res + (res / 2)), p.Y + rand.Next(-res, res + (res / 2)));
            }
        }
    }
}
