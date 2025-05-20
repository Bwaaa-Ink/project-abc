using Terminal.Gui;
using Attribute = Terminal.Gui.Attribute;

namespace Utilities.Console
{
    public enum ConsoleMode
    {
        Display,
        Single,
        Split
    }

    public class ConsoleOptions
    {
        public ConsoleMode Mode { get; set; }
        public bool SplitHorizontal { get; set; } = false;
        public int BufferSize { get; set; } = int.MaxValue;
        public Attribute ColorScheme { get; set; } =
            new(Color.White, Color.Black);
    }
}