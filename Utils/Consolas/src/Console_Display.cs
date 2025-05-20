using Terminal.Gui;

namespace Utilities.Console
{
    public class DisplayConsoleWindow : AbstractConsoleWindow
    {
        protected override void SetupViews()
        {
            _display = new TextView
            {
                ReadOnly = true,
                X = 0,
                Y = 0,
                Width = Dim.Fill(),
                Height = Dim.Fill()
            };
            _top.Add(_display);
        }
    }
}