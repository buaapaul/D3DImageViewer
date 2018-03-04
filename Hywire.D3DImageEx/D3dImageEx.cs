using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows.Interop;
using SlimDX;
using SlimD3D9 = SlimDX.Direct3D9;
using SlimD3D11 = SlimDX.Direct3D11;

namespace Hywire.D3DImageEx
{
    public enum D3DResourceType
    {
        //DX10Texture2D = 0,
        DX11Texture2D = 1,
    }
    public class test
    {
    }
    public class D3dImageEx : D3DImage,IDisposable
    {
        #region Private Fields
        private SlimD3D9.Direct3DEx _D3D9Ex;
        private SlimD3D9.DeviceEx _D3D9DeviceEx;
        private SlimD3D9.Texture _SharedTexture;
        #endregion Private Fields

        public D3dImageEx(IntPtr hWnd)
        {
            InitD3D9(hWnd);
        }

        #region Public Functions
        public void SetBackBufferEx(D3DResourceType resourceType, SlimD3D11.Texture2D resource)
        {
            if (resource == null)
            {
                SetBackBuffer(System.Windows.Interop.D3DResourceType.IDirect3DSurface9, IntPtr.Zero);
                return;
            }

            IntPtr sharedHandle = GetSharedHandle(resource);
            if (_SharedTexture != null)
            {
                _SharedTexture.Dispose();
                _SharedTexture = null;
            }
            GetSharedTexture(resource, ref sharedHandle, ref _SharedTexture);
            using (SlimD3D9.Surface d3d9Surface = _SharedTexture.GetSurfaceLevel(0))
            {
                Lock();
                SetBackBuffer(System.Windows.Interop.D3DResourceType.IDirect3DSurface9, d3d9Surface.ComPointer);
                Unlock();
            }
        }

        public void Dispose()
        {
            SafeRelease(_SharedTexture);
            SafeRelease(_D3D9DeviceEx);
            SafeRelease(_D3D9Ex);
        }
        #endregion Public Functions

        #region Private Functions
        private void InitD3D9(IntPtr hWnd)
        {
            SlimD3D9.PresentParameters d3dpp = new SlimD3D9.PresentParameters
            {
                Windowed = true,
                SwapEffect = SlimD3D9.SwapEffect.Discard,
                DeviceWindowHandle = hWnd,
                PresentationInterval = SlimDX.Direct3D9.PresentInterval.Immediate,
            };
            _D3D9Ex = new SlimD3D9.Direct3DEx();
            _D3D9DeviceEx = new SlimD3D9.DeviceEx(_D3D9Ex, 0, SlimD3D9.DeviceType.Hardware, hWnd,
                 SlimD3D9.CreateFlags.HardwareVertexProcessing | SlimD3D9.CreateFlags.FpuPreserve | SlimD3D9.CreateFlags.Multithreaded,
                 d3dpp
                 );
        }

        private IntPtr GetSharedHandle(SlimD3D11.Texture2D rscTex)
        {
            SlimDX.DXGI.Resource resource = new SlimDX.DXGI.Resource(rscTex);
            IntPtr result = resource.SharedHandle;

            resource.Dispose();

            return result;
        }

        private Result GetSharedTexture(SlimD3D11.Texture2D rscTex, ref IntPtr sharedHandle, ref SlimD3D9.Texture sharedTex)
        {
            SlimD3D9.Format format = ConvertDXGIToD3D9Format(rscTex.Description.Format);
            if (format == SlimD3D9.Format.Unknown)
            {
                return SlimD3D11.ResultCode.InvalidArgument;
            }
            sharedTex = new SlimD3D9.Texture(_D3D9DeviceEx, rscTex.Description.Width, rscTex.Description.Height, 1,
                SlimD3D9.Usage.RenderTarget, format, SlimD3D9.Pool.Default, ref sharedHandle);
            return SlimD3D11.ResultCode.Success;
        }

        private SlimD3D9.Format ConvertDXGIToD3D9Format(SlimDX.DXGI.Format srcFormat)
        {
            switch (srcFormat)
            {
                case SlimDX.DXGI.Format.B8G8R8A8_UNorm:
                    return SlimD3D9.Format.A8R8G8B8;
                case SlimDX.DXGI.Format.B8G8R8A8_UNorm_SRGB:
                    return SlimD3D9.Format.A8R8G8B8;
                case SlimDX.DXGI.Format.B8G8R8X8_UNorm:
                    return SlimD3D9.Format.X8R8G8B8;
                case SlimDX.DXGI.Format.R8G8B8A8_UNorm:
                    return SlimD3D9.Format.A8R8G8B8;
                case SlimDX.DXGI.Format.R8G8B8A8_UNorm_SRGB:
                    return SlimD3D9.Format.A8R8G8B8;
                default:
                    return SlimD3D9.Format.Unknown;
            }
        }
        private void SafeRelease(ComObject obj)
        {
            if (obj != null)
            {
                if (!obj.Disposed)
                {
                    obj.Dispose();
                }
            }
        }
        #endregion Private Functions
    }
}
