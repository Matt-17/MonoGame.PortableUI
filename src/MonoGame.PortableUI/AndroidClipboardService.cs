#if ANDROID
using Android.App;
using Android.Content;

namespace MonoGame.PortableUI
{
    /// <summary>
    /// <see cref="IClipboardService"/> backed by the Android system clipboard
    /// (<see cref="ClipboardManager"/>). Only compiled for the Android target framework.
    /// </summary>
    public sealed class AndroidClipboardService : IClipboardService
    {
        private static ClipboardManager? GetManager()
        {
            var context = Application.Context;
            return context?.GetSystemService(Context.ClipboardService) as ClipboardManager;
        }

        public string? GetText()
        {
            var manager = GetManager();
            if (manager is null || !manager.HasPrimaryClip)
                return null;

            var clip = manager.PrimaryClip;
            if (clip is null || clip.ItemCount == 0)
                return null;

            return clip.GetItemAt(0)?.CoerceToText(Application.Context);
        }

        public void SetText(string? text)
        {
            var manager = GetManager();
            if (manager is null)
                return;

            manager.PrimaryClip = ClipData.NewPlainText("text", text ?? string.Empty);
        }
    }
}
#endif
