using Terminal.Gui;

namespace Utilities.Console
{
    public class SingleConsoleWindow : AbstractConsoleWindow
    {
        protected override void SetupViews()
        {
            _display = new TextView
            {
                ReadOnly = true,
                X = 0,
                Y = 0,
                Width = Dim.Fill(),
                Height = Dim.Fill() - 1
            };
            var inputField = new TextField
            {
                X = 0,
                Y = Pos.AnchorEnd(1),
                Width = Dim.Fill(),
                Text = "> "
            };
            inputField.KeyDown += (args, key) => OnInputKey(args, key, inputField, null);
            _top.Add(_display, inputField);
        }
    }
}