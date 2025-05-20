using System;
using System.Diagnostics;
using System.IO;
using System.Linq;

namespace Utilities.Console
{
    public static class DefaultCommands
    {
        public static string DefaultPath { get; }

        static DefaultCommands()
            => DefaultPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments), "Logs", DateTimeOffset.Now.ToUnixTimeMilliseconds() + "_console.log");

        private static bool CheckPath(string path)
        {
            try
            {
                File.Create(path);
                File.Delete(path);
            }
            catch
            {
                return false;
            }

            return true;
        }

        private static string FullPath(string path)
            => Path.GetFullPath(path);

        public static void AddDefaults(IConsoleWindow console)
        {
            console.RegisterCommand(new Command("help", (string b) =>
            {
                console.WriteLine("COMMANDS:");
                foreach (var cmd in console.GetCommandList())
                    console.WriteLine(cmd);
            }, "list commands"));

            console.RegisterCommand(new Command("LogOutput", args =>
            {
                var path = !string.IsNullOrWhiteSpace(args) ? CheckPath(FullPath(args)) ? FullPath(args) : DefaultPath : DefaultPath;
                File.WriteAllText(path, console.GetBuffer());
                console.WriteLine($"Logged to {path}");
            }, "Logs the buffer to a file. Usage: LogOutput [string: file]"));

            console.RegisterCommand(new Command("SetBuffer", args =>
            {
                if (int.TryParse(args, out var size))
                {
                    if (size < 1)
                    {
                        console.WriteLine("Buffer must be >= 1.");
                        return;
                    }
                    console.WriteLine($"Setting buffer to {size} messages.");
                    console.SetBuffer(size);
                }
                else console.WriteLine("Failed to parse buffer, please provide an integer >= 1.");
            }, "Sets the window buffer of messages to store and use in saving. Usage: SetBuffer <int: size>"));

            console.RegisterCommand(new Command("GetMemoryUsage", _ =>
            {
                var m = Process.GetCurrentProcess().WorkingSet64;
                console.WriteLine(m.ToString());
            }, "GetMemoryUsage")
                );

            console.RegisterCommand(new Command("Clear", _ => console.Clear(), "Clear display"));
            console.RegisterCommand(new Command("Exit", _ => console.Stop(), "Exit console"));

            console.RegisterCommand(new Command("Time", _ =>
                    console.WriteLine(DateTime.Now.ToString("o")),
                "Current time"));

            console.RegisterCommand(new Command("Env", _ =>
            {
                foreach (var e in Environment.GetEnvironmentVariables()
                             .Cast<System.Collections.DictionaryEntry>())
                    console.WriteLine($"{e.Key}={e.Value}");
            }, "List env vars"));

            console.RegisterCommand(new Command("GCCollect", _ =>
            {
                GC.Collect();
                console.WriteLine("Collected");
            }, "Force GC"));
        }
    }
}