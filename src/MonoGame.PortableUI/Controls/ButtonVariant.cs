namespace MonoGame.PortableUI.Controls
{
    /// <summary>
    ///     The role of a button. Each non-standard variant resolves its chrome from its own theme slot
    ///     (<see cref="PortableTheme.PrimaryButton"/>, …), so themes can give call-to-action buttons
    ///     their era's look (glossy, beveled, chamfered) instead of a flat color fill.
    /// </summary>
    public enum ButtonVariant
    {
        Standard,
        Primary,
        Secondary,
        Danger
    }
}
