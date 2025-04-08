using System.Drawing;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Runtime.Versioning;
using static Windows.Win32.WinPI;

namespace PlatformInvokationWrappings
{
    public static class Extended
    {
        public static class Mouse
        {
            [SupportedOSPlatform("windows5.0")]
            public static Point Position {
                get
                {
                    GetCursorPos(out var point);
                    return point;
                }
                set => SetCursorPos(value.X, value.Y);
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void LogMarshalError(
            [CallerMemberName] string cname = "",
            [CallerLineNumber] int ln = 0
        )
        {
            var errorCode = Marshal.GetLastWin32Error();
            Console.WriteLine($"Marshal Erroring check called by {cname} @ {ln}");
            Console.WriteLine(errorCode != 0
                ? $">PINVOKE< Error: {errorCode} - {new System.ComponentModel.Win32Exception(errorCode).Message}"
                : "Marshal operation successful");
        }
    }
}
