using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Text;

namespace Hywire.D3DImageRenderer
{
    public struct ImageDisplayParameters
    {
        public float DisplayLimitHigh;
        public float DisplayLimitLow;
    }
    public class D3DWrapper
    {
        #region Private Fields
        private D3DRenderer _Renderer;
        private IntPtr _WindowHandle;
        private int _ImageWidth = 800;
        private int _ImageHeight = 600;
        private IntPtr _BackBuffer;
        #endregion Private Fields

        #region Public Properties
        public IntPtr BackBuffer
        {
            get { return _BackBuffer; }
        }
        #endregion Public Properties
        public D3DWrapper()
        {
            _Renderer = new D3DRenderer();
        }
        #region Public Functions
        public void Initialize(string imagePath, int imageWidth, int imageHeight, IntPtr hWnd)
        {
            _ImageWidth = imageWidth;
            _ImageHeight = imageHeight;
            _Renderer.Initialize(imagePath, _ImageWidth, _ImageHeight, hWnd);
            _BackBuffer = _Renderer.CreateSurface(_ImageWidth, _ImageHeight);
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
