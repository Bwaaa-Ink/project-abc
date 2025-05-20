using System;
using System.Collections.Generic;

namespace Utilities.Console
{
    public interface IConsoleWindow
    {
        void Initialize(ConsoleOptions options);
        void Write(string text);
        void WriteLine(string text);
        event Action<string> ReceivedInput;
        void Run();
        void Stop();

        void RegisterCommand(Command c);
        IEnumerable<string> GetCommandList();
        string GetBuffer();
        void SetBuffer(int size);
        void Clear();
    }
}