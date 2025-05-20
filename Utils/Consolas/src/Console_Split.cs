using Terminal.Gui;

namespace Utilities.Console
{
    public class SplitConsoleWindow : AbstractConsoleWindow
    {
        protected override void SetupViews()
        {
            var outputView = new TextView
            {
                ReadOnly = true,
                X = 0,
                Y = 0,
                Width = _options.SplitHorizontal ? Dim.Fill() : Dim.Percent(50),
                Height = _options.SplitHorizontal ? Dim.Percent(50) : Dim.Fill()
            };

            var inputView = new TextView
            {
                ReadOnly = false,

                X = _options.SplitHorizontal ? 0 : _top.GetContentSize().Width / 2,
                Y = _options.SplitHorizontal ? _top.GetContentSize().Height / 2 : 0,
                Width = Dim.Fill(),
                Height = _options.SplitHorizontal ? Dim.Fill() : Dim.Percent(50)
            };

            inputView.KeyDown += (args,key) => OnInputKey(args, key, null, inputView);

            _display = outputView;
            _top.Add(outputView, inputView);
            _top.ContentSizeChanged += (s, e) =>
            {
                var sizeN = e.Size;
                if (!sizeN.HasValue)
                    return;
                var size = sizeN.Value;
                inputView.X = _options.SplitHorizontal ? 0 : size.Width / 2;
                inputView.Y = _options.SplitHorizontal ? size.Height / 2 : 0;
            };
        }
    }
}