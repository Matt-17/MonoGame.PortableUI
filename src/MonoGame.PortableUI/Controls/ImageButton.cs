using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using MonoGame.PortableUI.Common;

namespace MonoGame.PortableUI.Controls
{
    public class ImageButton : Button
    {
        private readonly Image _image;

        public ImageButton()
        {
            _image = new Image();
            Content = _image;
        }

        public Texture2D Source
        {
            get { return _image.Source; }
            set { _image.Source = value; }
        }

        public Color TintColor
        {
            get { return _image.TintColor; }
            set { _image.TintColor = value; }
        }

        public Stretch Stretch
        {
            get { return _image.Stretch; }
            set { _image.Stretch = value; }
        }
    }
}
