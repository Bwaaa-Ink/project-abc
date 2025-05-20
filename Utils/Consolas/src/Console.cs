using System.Text;
using Terminal.Gui;

namespace Utilities.Console
{
	public abstract class AbstractConsoleWindow : IConsoleWindow
	{
		protected ConsoleOptions _options;
		protected Toplevel _top;
		protected TextView _display;
		readonly Dictionary<string, (Action<string> Handler, string Description)> _commands = new();
		protected StringBuilder _buffer = new();
		protected int _bufferLimit;

		public event Action<string> ReceivedInput = _ => { };

		public virtual void Initialize(ConsoleOptions options)
		{
			_options = options;
			_bufferLimit = options.BufferSize;
			Application.Init();
			_top = new Window() { CanFocus = true, BorderStyle = LineStyle.Rounded, ShadowStyle = ShadowStyle.Transparent, Title = DefaultCommands.DefaultPath, ColorScheme = new ColorScheme { Normal = options.ColorScheme } };
			SetupViews();
			DefaultCommands.AddDefaults(this);
			Application.Begin(_top);
		}

		protected abstract void SetupViews();

		public virtual void Write(string text)
		{
			_buffer.Append(text);
			TrimBuffer();
			_display.Text = _buffer.ToString();
		}

		public virtual void WriteLine(string text)
		{
			_buffer.Append(text).Append('\n');
			TrimBuffer();
			_display.Text = _buffer.ToString();
		}

		void TrimBuffer()
		{
			if (_buffer.Length > _bufferLimit)
				_buffer.Remove(0, _buffer.Length - _bufferLimit);
		}

		public void RegisterCommand(Command c)
			=> _commands[c.Name] = (c.Func, c.Description);

		public IEnumerable<string> GetCommandList()
			=> _commands.Select(kv =>
			   kv.Key + kv.Value.Description);

		public string GetBuffer() => _buffer.ToString();

		public void SetBuffer(int size)
		{
			_bufferLimit = size;
			TrimBuffer();
			_display.Text = _buffer.ToString();
		}

		public void Clear()
		{
			_buffer.Clear();
			_display.Text = string.Empty;
		}

		protected void OnInputKey(object? sender, Key key, TextField? input = null, TextView? inputView = null)
		{
			var inputActual = (View?)input ?? inputView;
			if (key == Key.Enter)
			{
				var text = input != null
					? input.Text.Substring(2)
					: inputView.Text;

				var parts = text
					.Split(' ', StringSplitOptions.RemoveEmptyEntries);

				if (parts.Length > 0 && _commands.TryGetValue(parts[0], out var cmd))
				{
					WriteLine(parts[0]);
					cmd.Handler(string.Join(" ", parts.Skip(1)));
				}
				else WriteLine($"Unknown command: {parts.FirstOrDefault() ?? ""}");

				if (inputActual != null)
					inputActual.Text = "> ";

				ReceivedInput(text);
			}
			else if (key == Key.Backspace)
			{
				if (inputActual == null)
					return;
				if (inputActual.Text.Length > 2)
					inputActual.Text = inputActual.Text[..^2];
			}
			else if (key.IsKeyCodeAtoZ || (key.KeyCode >= Key.D0.KeyCode && key.KeyCode >= Key.D9.KeyCode))
			{
				if (inputActual == null)
					return;
				inputActual.Text += key.AsRune == default ? "" : key.AsRune;
			}

			key.Handled = true;
		}

		public void Run() => Application.Run(_top);
		public void Stop() => Application.RequestStop();
	}
}
