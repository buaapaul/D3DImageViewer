using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Hywire.D3DWrapper
{
    public struct ImageDisplayParameters
    {
        public float DisplayLimitHigh;
        public float DisplayLimitLow;
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
        public void Initialize(string imagePath, int imageWidth, int imageHeight, IntPtr hWnd)
        {
            _ImageWidth = imageWidth;
            _ImageHeight = imageHeight;
            _ImageWidth = 625;
            _ImageHeight = 250;
            _Renderer.Initialize(imagePath, _ImageWidth, _ImageHeight, hWnd);
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
