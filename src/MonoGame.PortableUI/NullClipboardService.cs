namespace MonoGame.PortableUI
{
    public sealed class NullClipboardService : IClipboardService
    {
        public static NullClipboardService Instance { get; } = new NullClipboardService();

        private NullClipboardService()
        {
        }

        public string? GetText()
        {
            return null;
        }

        public void SetText(string? text)
        {
        }
    }
}
