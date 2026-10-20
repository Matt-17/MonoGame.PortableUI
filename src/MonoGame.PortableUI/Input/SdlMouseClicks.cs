#if !ANDROID
using System;
using System.Runtime.InteropServices;
using System.Threading;

namespace MonoGame.PortableUI.Input
{
    /// <summary>
    ///     Counts mouse button presses as SDL reports them. <c>Mouse.GetState()</c> only gives the buttons' state at
    ///     the moment of the poll, so a click pressed and released between two polls (a quick click during a slow
    ///     frame) never showed as pressed. An SDL event watch sees every press; <see cref="DeviceInputSource"/>
    ///     takes the counts once per frame. Without the SDL that DesktopGL drives, nothing is counted and the
    ///     source keeps polling.
    /// </summary>
    internal static class SdlMouseClicks
    {
        internal const int Left = 1, Middle = 2, Right = 3;

        private const uint ButtonDown = 0x401;   // SDL_MOUSEBUTTONDOWN
        private const int ButtonOffset = 16;     // SDL_MouseButtonEvent: type, timestamp, windowID, which (4 bytes each), button
        private static readonly string[] LibraryNames = { "SDL2", "libSDL2-2.0.so.0", "libSDL2-2.0.0.dylib", "libSDL2.dylib" };
        private static readonly int[] Presses = new int[4];

        // Kept alive here: SDL holds only the function pointer.
        private static EventFilter? _watch;
        private static WasInitFunction? _wasInit;
        private static IntPtr _library;
        private static bool _unavailable;

        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        private delegate int EventFilter(IntPtr userData, IntPtr sdlEvent);

        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        private delegate void AddEventWatchFunction(IntPtr filter, IntPtr userData);

        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        private delegate uint WasInitFunction(uint flags);

        /// <summary>True once the watch runs in the SDL that MonoGame drives.</summary>
        internal static bool Installed { get; private set; }

        /// <summary>
        ///     Starts counting once SDL is running; returns false while it is not (yet). Cheap to call every frame:
        ///     without an SDL library it gives up for good.
        /// </summary>
        internal static bool TryInstall()
        {
            if (Installed || _unavailable)
                return Installed;
            try
            {
                if (_wasInit == null)
                {
                    if (!TryLoad(out _library)
                        || !NativeLibrary.TryGetExport(_library, "SDL_WasInit", out var wasInit))
                    {
                        _unavailable = true;
                        return false;
                    }
                    _wasInit = Marshal.GetDelegateForFunctionPointer<WasInitFunction>(wasInit);
                }
                // An SDL nobody initialised is not the one the game pumps: its events would never arrive.
                if (_wasInit(0) == 0)
                    return false;
                if (!NativeLibrary.TryGetExport(_library, "SDL_AddEventWatch", out var addEventWatch))
                {
                    _unavailable = true;
                    return false;
                }
                _watch = OnEvent;
                Marshal.GetDelegateForFunctionPointer<AddEventWatchFunction>(addEventWatch)(Marshal.GetFunctionPointerForDelegate(_watch), IntPtr.Zero);
                Installed = true;
            }
            catch (Exception e) when (e is DllNotFoundException or EntryPointNotFoundException or BadImageFormatException)
            {
                _unavailable = true;
            }
            return Installed;
        }

        private static bool TryLoad(out IntPtr library)
        {
            // MonoGame loaded SDL by its full path already; loading by name returns the same module.
            foreach (var name in LibraryNames)
            {
                if (NativeLibrary.TryLoad(name, typeof(SdlMouseClicks).Assembly, null, out library))
                    return true;
            }
            library = IntPtr.Zero;
            return false;
        }

        private static int OnEvent(IntPtr userData, IntPtr sdlEvent)
        {
            if ((uint)Marshal.ReadInt32(sdlEvent) == ButtonDown)
                Record(Marshal.ReadByte(sdlEvent, ButtonOffset));
            return 1;
        }

        internal static void Record(int button)
        {
            if (button is >= Left and <= Right)
                Interlocked.Increment(ref Presses[button]);
        }

        /// <summary>The presses of <paramref name="button"/> since the last call.</summary>
        internal static int Take(int button) => Interlocked.Exchange(ref Presses[button], 0);

        /// <summary>Drops the presses counted so far (they happened while nobody read the device).</summary>
        internal static void Discard()
        {
            Take(Left);
            Take(Middle);
            Take(Right);
        }
    }
}
#endif
