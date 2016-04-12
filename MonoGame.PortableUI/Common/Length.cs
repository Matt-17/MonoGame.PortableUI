namespace MonoGame.PortableUI.Common
{
    public struct Length
    {
        private enum LengthTypeEnum
        {
            Value = 0,
            Wrap,
            Stretch
        }

        public static Length WrapContent = new Length (LengthTypeEnum.Wrap);
        public static Length StretchContent = new Length (LengthTypeEnum.Stretch);

        private Length(LengthTypeEnum lengthTypeEnum)
        {
            LengthType = lengthTypeEnum;
        }

        private LengthTypeEnum LengthType { get; }
    }
}