using System;
using System.Runtime.InteropServices;

namespace MonoGame.PortableUI
{
    public sealed class WindowsClipboardService : IClipboardService
    {
        private const uint CfUnicodeText = 13;
        private const uint GmemMoveable = 0x0002;

        public string? GetText()
        {
            if (!OperatingSystem.IsWindows() || !OpenClipboard(IntPtr.Zero))
                return null;

            try
            {
                if (!IsClipboardFormatAvailable(CfUnicodeText))
                    return null;

                var handle = GetClipboardData(CfUnicodeText);
                if (handle == IntPtr.Zero)
                    return null;

                var pointer = GlobalLock(handle);
                if (pointer == IntPtr.Zero)
                    return null;

                try
                {
                    return Marshal.PtrToStringUni(pointer);
                }
                finally
                {
                    GlobalUnlock(handle);
                }
            }
            finally
            {
                CloseClipboard();
            }
        }

        public void SetText(string? text)
        {
            if (!OperatingSystem.IsWindows() || !OpenClipboard(IntPtr.Zero))
                return;

            IntPtr handle = IntPtr.Zero;
            try
            {
                var value = text ?? "";
                var chars = (value + '\0').ToCharArray();
                handle = GlobalAlloc(GmemMoveable, (UIntPtr)(chars.Length * sizeof(char)));
                if (handle == IntPtr.Zero)
                    return;

                var pointer = GlobalLock(handle);
                if (pointer == IntPtr.Zero)
                    return;

                try
                {
                    Marshal.Copy(chars, 0, pointer, chars.Length);
                }
                finally
                {
                    GlobalUnlock(handle);
                }

                EmptyClipboard();
                if (SetClipboardData(CfUnicodeText, handle) != IntPtr.Zero)
                    handle = IntPtr.Zero;
            }
            finally
            {
                if (handle != IntPtr.Zero)
                    GlobalFree(handle);
                CloseClipboard();
            }
        }

        [DllImport("user32.dll", SetLastError = true)]
        private static extern bool OpenClipboard(IntPtr hWndNewOwner);

        [DllImport("user32.dll", SetLastError = true)]
        private static extern bool CloseClipboard();

        [DllImport("user32.dll", SetLastError = true)]
        private static extern bool EmptyClipboard();

        [DllImport("user32.dll", SetLastError = true)]
        private static extern bool IsClipboardFormatAvailable(uint format);

        [DllImport("user32.dll", SetLastError = true)]
        private static extern IntPtr GetClipboardData(uint uFormat);

        [DllImport("user32.dll", SetLastError = true)]
        private static extern IntPtr SetClipboardData(uint uFormat, IntPtr hMem);

        [DllImport("kernel32.dll", SetLastError = true)]
        private static extern IntPtr GlobalAlloc(uint uFlags, UIntPtr dwBytes);

        [DllImport("kernel32.dll", SetLastError = true)]
        private static extern IntPtr GlobalLock(IntPtr hMem);

        [DllImport("kernel32.dll", SetLastError = true)]
        private static extern bool GlobalUnlock(IntPtr hMem);

        [DllImport("kernel32.dll", SetLastError = true)]
        private static extern IntPtr GlobalFree(IntPtr hMem);
    }
}
