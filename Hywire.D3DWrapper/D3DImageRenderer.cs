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
    public struct ImageDisplayParameterStruct
    {
        public float DisplayLimitHigh;
        public float DisplayLimitLow;
        public Vector3 ViewerPosition;
        public int RedChannelMap;
        public int GreenChannelMap;
        public int BlueChannelMap;
        public int AlphaChannelMap;
    }
    public class D3DImageRenderer
    {
        #region Private Fields
        private DirectXWrapper _D3dWrapper;
        private IntPtr _BackBuffer;
        private bool _IsInitialized;
        #endregion Private Fields

        #region Public Properties
        public IntPtr BackBuffer
        {
            get { return _BackBuffer; }
        }

        public bool IsInitialized
        {
            get
            {
                return _IsInitialized;
            }
        }
        #endregion Public Properties
        public D3DImageRenderer()
        {
            _D3dWrapper = new DirectXWrapper();
        }
        #region Public Functions
        public void Initialize(ImageInfo imageInfo, IntPtr hWnd)
        {
            _D3dWrapper.Initialize(imageInfo, hWnd);
            _BackBuffer = _D3dWrapper.SurfacePointer;
            _IsInitialized = true;
        }
        public void Draw(ImageDisplayParameterStruct displayParameters)
        {
            _D3dWrapper.Draw(displayParameters);
        }
        public void CleanUp()
        {
            _D3dWrapper.CleanUp();
            _IsInitialized = false;
        }
        #endregion Public Functions
    }
}
