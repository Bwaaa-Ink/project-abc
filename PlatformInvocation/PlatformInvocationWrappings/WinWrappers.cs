using System.Runtime.Versioning;
using Windows.Win32;
using Windows.Win32.UI.WindowsAndMessaging;

namespace PlatformInvokationWrappings
{
    [SupportedOSPlatform("windows5.0")]
    public static class WinWrappers
    {
        public static bool UseUnicode { get; set; } = true;

        /// <summary>
        /// Toggles the cursor status
        /// </summary>
        public static void ToggleCursor()
        {
            var current = WinPI.ShowCursor(true);
            if (current == 0)
                return;
            if (current < 0)
                while (WinPI.ShowCursor(true) < 0);
            else while (WinPI.ShowCursor(false) > 0);
        }

        public static (int, int) GetPrimaryWorkspaceWidthHeight()
            => (
                WinPI.GetSystemMetrics(SYSTEM_METRICS_INDEX.SM_CXSCREEN),
                WinPI.GetSystemMetrics(SYSTEM_METRICS_INDEX.SM_CYSCREEN)
            );

        public static void SetMousePos(int x, int y)
            => WinPI.SetCursorPos(x, y);

        public static Structs.POINT<int> GetMousePos()
        {
            WinPI.GetCursorPos(out var p);
            return p;
        }
    }
}
