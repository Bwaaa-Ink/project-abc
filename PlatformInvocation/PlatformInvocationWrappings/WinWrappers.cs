using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Runtime.Versioning;
using Windows.Win32;
using Windows.Win32.Foundation;
using Windows.Win32.UI.Input.KeyboardAndMouse;
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

        public static void SetMousePos(Structs.POINT<int> point)
            => SetMousePos(point.X, point.Y);

        public static Structs.POINT<int> GetMousePos()
        {
            WinPI.GetCursorPos(out var p);
            return p;
        }

        private static HHOOK _hookId = HHOOK.Null;
        
        public static void StopMouseInput()
        {
            if (_hookId == HHOOK.Null)
                _hookId = SetHook();
        }

        public static void ResumeMouseInput()
        {
            if (_hookId != HHOOK.Null)
                WinPI.UnhookWindowsHookEx(_hookId);
            _hookId = HHOOK.Null;
        }

        private static HHOOK SetHook()
        {
            using var prc = Process.GetCurrentProcess();
            using var mdl = prc.MainModule;
            var safeHandle = WinPI.SetWindowsHookEx(
                WINDOWS_HOOK_ID.WH_MOUSE_LL,
                _hook,
                WinPI.GetModuleHandle(mdl.ModuleName),
                0);

            IntPtr raw = safeHandle.DangerousGetHandle();
            return new HHOOK(raw);
        }

        private static readonly HOOKPROC _hook = HookCB;

        private static LRESULT HookCB(int code, WPARAM wParam, LPARAM lParam)
        {
            if (code >= 0)
                return (LRESULT)((nint)1);
            return WinPI.CallNextHookEx(_hookId, code, wParam, lParam);
        }
    }
}
