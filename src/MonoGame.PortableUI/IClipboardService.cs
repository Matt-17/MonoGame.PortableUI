namespace MonoGame.PortableUI
{
    public interface IClipboardService
    {
        string? GetText();
        void SetText(string? text);
    }
}
