using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using SlimDX;
using System.IO;
//using System.Drawing.Imaging;

namespace Hywire.D3DWrapper
{
    public struct ImageDisplayParameters
    {
        public float DisplayLimitHigh;
        public float DisplayLimitLow;
        public Vector3 ViewerPosition;
    }
    public class DirectXWrapper
    {
        #region Private Fields
        private D3DRenderer _Renderer;
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
        public void Initialize(ImageInfo imageInfo, IntPtr hWnd)
        {
            _Renderer.Initialize(imageInfo, hWnd);
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
