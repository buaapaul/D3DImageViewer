using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using D3DX9 = SlimDX.Direct3D9;

namespace Hywire.D3DWrapper
{
    public class ImageInfo : IDisposable
    {
        private int _Stride;
        private int _Width;
        private int _Height;
        private D3DX9.Format _Format;
        private BitmapImage _Image;
        private int _BytesPerPixel;

        public ImageInfo(BitmapImage image)
        {
            int bytesPerRow = image.PixelWidth * image.Format.BitsPerPixel / 8;
            _Stride = bytesPerRow + (bytesPerRow % 4 == 0 ? 0 : (4 - bytesPerRow % 4));
            _Width = image.PixelWidth;
            _Height = image.PixelHeight;
            _Format = ConvertToTextureFormat(image.Format);
            _Image = image;
            _BytesPerPixel = image.Format.BitsPerPixel / 8;
        }

        public int BackBufferStride { get { return _Stride; } }
        public int PixelWidth { get { return _Width; } }
        public int PixelHeight { get { return _Height; } }
        public D3DX9.Format PixelFormat { get { return _Format; } }
        public BitmapImage Image { get { return _Image; } }
        public int BytesPerPixel { get { return _BytesPerPixel; } }

        public static D3DX9.Format ConvertToTextureFormat(PixelFormat imageFormat)
        {
            D3DX9.Format texFormat=new D3DX9.Format();
            if (imageFormat == PixelFormats.Gray16)
            {
                texFormat = D3DX9.Format.L16;
            }
            else if (imageFormat == PixelFormats.Gray8)
            {
                texFormat = D3DX9.Format.L8;
            }
            else if (imageFormat == PixelFormats.Rgba64)
            {
                texFormat = D3DX9.Format.A16B16G16R16;
            }
            else if (imageFormat == PixelFormats.Bgra32)
            {
                texFormat = D3DX9.Format.A8R8G8B8;
            }
            else if (imageFormat == PixelFormats.Bgr32)
            {
                texFormat = D3DX9.Format.X8R8G8B8;
            }
            else if (imageFormat == PixelFormats.Bgr24)
            {
                texFormat = D3DX9.Format.X8R8G8B8;
            }
            else
            {
                throw new NotSupportedException("Unsurpported image format!");
            }

            return texFormat;
        }
        public void Dispose()
        {
            _Image.StreamSource.Dispose();
            _Image.UriSource = null;
            _Image.BaseUri = null;
            _Image = null;
        }
    }
}
