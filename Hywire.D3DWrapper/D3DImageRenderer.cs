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
    public enum DirectXVersion
    {
        DirectX9 = 0,
        //DirectX10 = 1,
        DirectX11 = 2,
    }
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
        private DirectX11Wrapper _D3d11Wrapper;
        private IntPtr _BackBuffer;
        private bool _IsInitialized;
        private DirectXVersion _AppliedDXVersion;
        #endregion Private Fields

        #region Public Properties
        public IntPtr BackBuffer
        {
            get
            {
                if (_AppliedDXVersion == DirectXVersion.DirectX9)
                {
                    return _BackBuffer;
                }
                else { return IntPtr.Zero; }
            }
        }

        public SlimDX.Direct3D11.Texture2D RenderTarget
        {
            get
            {
                if (_AppliedDXVersion == DirectXVersion.DirectX11)
                {
                    return _D3d11Wrapper.RenderTargetTexture;
                }
                else { return null; }
            }
        }

        public bool IsInitialized
        {
            get
            {
                return _IsInitialized;
            }
        }
        #endregion Public Properties
        public D3DImageRenderer(DirectXVersion dxVersion = DirectXVersion.DirectX9)
        {
            if(dxVersion== DirectXVersion.DirectX11)
            {
                _D3d11Wrapper = new DirectX11Wrapper();
                _AppliedDXVersion = DirectXVersion.DirectX11;
            }
            else
            {
                _D3dWrapper = new DirectXWrapper();
                _AppliedDXVersion = DirectXVersion.DirectX9;
            }
        }
        #region Public Functions
        public void Initialize(ImageInfo imageInfo, IntPtr hWnd)
        {
            if (_AppliedDXVersion == DirectXVersion.DirectX9)
            {
                _D3dWrapper.Initialize(imageInfo, hWnd);
                _BackBuffer = _D3dWrapper.SurfacePointer;
                _IsInitialized = true;
            }
            else
            {
                _D3d11Wrapper.Initialize(imageInfo, hWnd);
                _IsInitialized = true;
            }
        }
        public void Draw(ImageDisplayParameterStruct displayParameters)
        {
            if (_AppliedDXVersion == DirectXVersion.DirectX9)
            {
                _D3dWrapper.Draw(displayParameters);
            }
            else if (_AppliedDXVersion == DirectXVersion.DirectX11)
            {
                _D3d11Wrapper.Draw(displayParameters);
            }
        }
        public void CleanUp()
        {
            if (_AppliedDXVersion == DirectXVersion.DirectX9)
            {
                _D3dWrapper.CleanUp();
            }
            else if (_AppliedDXVersion == DirectXVersion.DirectX11)
            {
                _D3d11Wrapper.CleanUp();
            }
            _IsInitialized = false;
        }
        #endregion Public Functions
    }
}
