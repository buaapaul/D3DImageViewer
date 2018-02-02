using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Windows.Media.Imaging;

namespace Hywire.D3DWrapper
{
    public struct ImageDisplayParameters
    {
        public float DisplayLimitHigh;
        public float DisplayLimitLow;
        public float CameraPosition;
    }
    public class DirectXWrapper
    {
        #region Private Fields
        private D3DRenderer _Renderer;
        private int _ImageWidth;
        private int _ImageHeight;
        private IntPtr _BackBuffer;
        #endregion Private Fields

        #region Public Properties
        public IntPtr BackBuffer
        {
            get { return _BackBuffer; }
        }
        #endregion Public Properties
        public DirectXWrapper()
        {
            _Renderer = new D3DRenderer();
        }
        #region Public Functions
        public void Initialize(BitmapImage image, int imageWidth, int imageHeight, IntPtr hWnd)
        {
            _ImageWidth = imageWidth;
            _ImageHeight = imageHeight;
            _Renderer.Initialize(image, _ImageWidth, _ImageHeight, hWnd);
            _BackBuffer = _Renderer.SurfacePointer;
        }
        public void Draw(ImageDisplayParameters displayParameters)
        {
            _Renderer.Draw(displayParameters);
        }
        public void CleanUp()
        {
            _Renderer.CleanUp();
        }
        #endregion Public Functions
    }
}
