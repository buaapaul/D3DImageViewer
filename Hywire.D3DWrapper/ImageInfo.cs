using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows.Media;
using System.Windows.Media.Imaging;

namespace Hywire.D3DWrapper
{
    public class ImageInfo
    {
        private int _Stride;
        private int _Width;
        private int _Height;
        private PixelFormat _Format;
        private BitmapImage _Image;

        public ImageInfo(BitmapImage image)
        {
            int bytesPerRow = image.PixelWidth * image.Format.BitsPerPixel / 8;
            _Stride = bytesPerRow + (bytesPerRow % 4 == 0 ? 0 : (4 - bytesPerRow % 4));
            _Width = image.PixelWidth;
            _Height = image.PixelHeight;
            _Format = image.Format;
            _Image = image;
        }

        public int BackBufferStride { get { return _Stride; } }
        public int PixelWidth { get { return _Width; } }
        public int PixelHeight { get { return _Height; } }
        public PixelFormat Format { get { return _Format; } }
        public BitmapImage Image { get { return _Image; } }
    }
}
