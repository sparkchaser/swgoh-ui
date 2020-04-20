using System;
using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Interop;

namespace goh_ui
{
    /// <summary>
    /// A modal sub-window with a 'Close' button.
    /// </summary>
    public class ToolWindow : Window
    {
        public SimpleCommand CloseClick { get; private set; }

        static ToolWindow()
        {
            // Load our custom style as the default for this class
            DefaultStyleKeyProperty.OverrideMetadata(typeof(ToolWindow),
                new FrameworkPropertyMetadata(typeof(ToolWindow)));
        }
        
        public ToolWindow()
        {
            // Wire up close button
            CloseClick = new SimpleCommand(Close);

            // Prevent window from being minimized/maximized
            WindowInteropHelper wih = new WindowInteropHelper(this);
            uint style = (uint)GetWindowLongPtr(wih.EnsureHandle(), GWL_STYLE);
            style &= ~NO_MAX_OR_MIN;
            SetWindowLongPtr(wih.Handle, GWL_STYLE, (IntPtr)style);
            SetWindowPos(wih.Handle, IntPtr.Zero, 0, 0, 0, 0, SWP_FLAGS);
        }

        #region Win32 Interop

        [DllImport("user32.dll", EntryPoint = "GetWindowLong")]
        private static extern IntPtr GetWindowLongPtr32(IntPtr hWnd, int nIndex);

        [DllImport("user32.dll", EntryPoint = "GetWindowLongPtr")]
        private static extern IntPtr GetWindowLongPtr64(IntPtr hWnd, int nIndex);
        [DllImport("user32.dll", EntryPoint = "SetWindowLong")]
        private static extern int SetWindowLong32(IntPtr hWnd, int nIndex, int dwNewLong);

        [DllImport("user32.dll", EntryPoint = "SetWindowLongPtr")]
        private static extern IntPtr SetWindowLongPtr64(IntPtr hWnd, int nIndex, IntPtr dwNewLong);

        [DllImport("user32.dll", SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        private static extern bool SetWindowPos(IntPtr hWnd, IntPtr hWndInsertAfter, int x, int y, int cx, int cy, uint uFlags);

        private static IntPtr GetWindowLongPtr(IntPtr hWnd, int nIndex) => (IntPtr.Size == 8) ? GetWindowLongPtr64(hWnd, nIndex) : GetWindowLongPtr32(hWnd, nIndex);

        private static IntPtr SetWindowLongPtr(IntPtr hWnd, int nIndex, IntPtr dwNewLong) => (IntPtr.Size == 8) ? SetWindowLongPtr64(hWnd, nIndex, dwNewLong) : new IntPtr(SetWindowLong32(hWnd, nIndex, dwNewLong.ToInt32()));

        #region Constants

        private const int GWL_STYLE = (-16);

        private const uint NO_MAX_OR_MIN    = 0x10000 | 0x20000;

        private const int SWP_NOOWNERZORDER = 0x0200;
        private const int SWP_NOREPOSITION  = 0x0200;
        private const int SWP_FRAMECHANGED  = 0x0020;
        private const int SWP_NOACTIVATE    = 0x0010;
        private const int SWP_NOZORDER      = 0x0004;
        private const int SWP_NOMOVE        = 0x0002;
        private const int SWP_NOSIZE        = 0x0001;

        private const int SWP_FLAGS = (SWP_FRAMECHANGED | SWP_NOACTIVATE | SWP_NOMOVE | SWP_NOOWNERZORDER | SWP_NOREPOSITION | SWP_NOSIZE | SWP_NOZORDER);

        #endregion
        #endregion
    }
}
