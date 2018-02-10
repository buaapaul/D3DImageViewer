using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using SlimDX;
using SlimDX.Direct3D9;
using System.Drawing;
using System.Windows.Media.Imaging;
using System.IO;
using System.Drawing.Imaging;
using System.Reflection;

namespace Hywire.D3DWrapper
{
    internal class DirectXWrapper
    {
        #region Private Fields
        private Direct3D _Direct3D;
        private Device _Device;
        private Effect _Effect;
        private Texture[] _Textures;
        private Surface _Surface;
        private VertexBuffer _VertexBuffer;
        private int _VertexStride;
        private Matrix _WorldViewProj;
        private struct CustomVertex
        {
            public Vector3 Pos;
            public Vector2 Tex;
        }
        private const VertexFormat CUSTOMVERTEXFORMAT = VertexFormat.Position | VertexFormat.Texture1;

        private int _TextureHorizontalDivide = 4;
        private int _TextureVerticalDivide = 4;
        #endregion Private Fields
        #region Public Properties
        public IntPtr SurfacePointer
        {
            get; private set;
        }
        #endregion Public Properties

        #region Public Functions
        public Result Initialize(ImageInfo imageInfo, IntPtr hWnd)
        {
            _Direct3D = new Direct3D();
            if (Result.Last.IsFailure)
            {
                return Result.Last;
            }
            PresentParameters d3dpp = new PresentParameters()
            {
                Windowed = true,
                SwapEffect = SwapEffect.Discard,
                BackBufferFormat = Format.Unknown,
                EnableAutoDepthStencil = false,
                AutoDepthStencilFormat = Format.D16,
                //Multisample = MultisampleType.TwoSamples,
                //MultisampleQuality = 0,
            };
            _Device = new Device(_Direct3D, 0, DeviceType.Hardware, hWnd,
                CreateFlags.HardwareVertexProcessing | CreateFlags.Multithreaded | CreateFlags.FpuPreserve, d3dpp);
            _Device.SetRenderState(RenderState.CullMode, Cull.None);
            _Device.SetRenderState(RenderState.Lighting, false);
            _Device.SetRenderState(RenderState.ZEnable, false);
            _Device.SetRenderState(RenderState.AlphaBlendEnable, true);
            //_Device.SetRenderState(RenderState.MultisampleAntialias, true);

            CreateTextures(imageInfo);

            SurfacePointer = CreateSurface(imageInfo.PixelWidth, imageInfo.PixelHeight);

            InitVertices();

            Stream effectStream = Assembly.GetExecutingAssembly().GetManifestResourceStream("Hywire.D3DWrapper.DisplayShader.fx");
            _Effect = Effect.FromStream(_Device, effectStream, ShaderFlags.NoPreshader);

            return Result.Last;
        }
        public void Draw(ImageDisplayParameters displayParameters)
        {
            _Device.Clear(ClearFlags.Target, Color.Transparent.ToArgb(), 1.0f, 0);
            if (_Device.BeginScene().IsSuccess)
            {
                SetupMatrices(displayParameters.ViewerPosition);
                _Device.SetStreamSource(0, _VertexBuffer, 0, _VertexStride);
                _Device.VertexFormat = CUSTOMVERTEXFORMAT;

                for (int i = 0; i < _TextureVerticalDivide; i++)
                {
                    for(int j = 0; j < _TextureHorizontalDivide; j++)
                    {
                        _Effect.SetTexture(_Effect.GetParameter(null, "g_Texture"), _Textures[i * _TextureHorizontalDivide + j]);
                        _Effect.SetValue(_Effect.GetParameter(null, "g_DisplayRangeH"), displayParameters.DisplayLimitHigh);
                        _Effect.SetValue(_Effect.GetParameter(null, "g_DisplayRangeL"), displayParameters.DisplayLimitLow);
                        _Effect.SetValue(_Effect.GetParameter(null, "g_mWorldViewProjection"), _WorldViewProj);
                        _Effect.Technique = _Effect.GetTechnique("DefaultRenderingTechnique");
                        if (Result.Last.IsFailure)
                        {
                            return;
                        }
                        _Effect.Begin();
                        if (Result.Last.IsFailure)
                        {
                            return;
                        }
                        _Effect.BeginPass(0);
                        _Device.DrawPrimitives(PrimitiveType.TriangleStrip, (i * _TextureHorizontalDivide + j) * 4, 2);
                        _Effect.EndPass();
                        _Effect.End();
                    }
                }

                _Device.EndScene();
            }
        }
        public void CleanUp()
        {
            if (_Textures != null)
            {
                for(int i = 0; i < _Textures.Length; i++)
                {
                    SafeRelease(_Textures[i]);
                }
                _Textures = null;
            }
            SafeRelease(_Surface);
            SafeRelease(_Effect);
            SafeRelease(_VertexBuffer);
            SafeRelease(_Device);
            SafeRelease(_Direct3D);
        }
        #endregion Public Functions

        #region Private Functions
        private Result CreateTextures(ImageInfo imageInfo)
        {
            // check for max allowed texture size, divide the image if needed
            Capabilities devCaps = _Direct3D.GetDeviceCaps(0, DeviceType.Hardware);
            if (imageInfo.PixelWidth > devCaps.MaxTextureWidth)
            {
                _TextureHorizontalDivide = (int)(imageInfo.PixelWidth / (double)devCaps.MaxTextureWidth) + 1;
            }
            else { _TextureHorizontalDivide = 1; }
            if (imageInfo.PixelHeight > devCaps.MaxTextureHeight)
            {
                _TextureVerticalDivide = (int)(imageInfo.PixelHeight / (double)devCaps.MaxTextureHeight) + 1;
            }
            else { _TextureVerticalDivide = 1; }
            if (_Textures != null)
            {
                for(int i = 0; i < _Textures.Length; i++)
                {
                    SafeRelease(_Textures[i]);
                }
            }
            _Textures = new Texture[_TextureHorizontalDivide * _TextureVerticalDivide];

            int texAverageWidth = imageInfo.PixelWidth / _TextureHorizontalDivide;
            int texAverageHeight = imageInfo.PixelHeight / _TextureVerticalDivide;
            System.Windows.Int32Rect imgRect = new System.Windows.Int32Rect();
            byte[] pixels = new byte[imageInfo.BackBufferStride * imageInfo.PixelHeight];
            DataRectangle texDataRect;

            for(int i = 0; i < _TextureVerticalDivide; i++)
            {
                for(int j = 0; j < _TextureHorizontalDivide; j++)
                {
                    if (j == 0)
                    {
                        imgRect.Width = texAverageWidth;
                        imgRect.X = 0;
                    }
                    else if (j == _TextureHorizontalDivide - 1)
                    {
                        imgRect.Width = imageInfo.PixelWidth - (_TextureHorizontalDivide - 1) * texAverageWidth + 1;
                        imgRect.X = j * texAverageWidth - 1;
                    }
                    else
                    {
                        imgRect.Width = texAverageWidth + 1;
                        imgRect.X = j * texAverageWidth - 1;
                    }
                    if (i == 0)
                    {
                        imgRect.Height = texAverageHeight;
                        imgRect.Y = 0;
                    }
                    else if (i == _TextureVerticalDivide - 1)
                    {
                        imgRect.Height = imageInfo.PixelHeight - (_TextureVerticalDivide - 1) * texAverageHeight + 1;
                        imgRect.Y = i * texAverageHeight - 1;
                    }
                    else
                    {
                        imgRect.Height = texAverageHeight + 1;
                        imgRect.Y = i * texAverageHeight - 1;
                    }
                    Texture tmpTexture = new Texture(_Device, imgRect.Width, imgRect.Height, 1, Usage.None, imageInfo.PixelFormat, Pool.SystemMemory);
                    _Textures[i * _TextureHorizontalDivide + j] = new Texture(_Device, imgRect.Width, imgRect.Height, 1, Usage.None, imageInfo.PixelFormat, Pool.Default);
                    imageInfo.Image.CopyPixels(imgRect, pixels, imgRect.Width * imageInfo.BytesPerPixel, 0);
                    texDataRect = tmpTexture.LockRectangle(0, LockFlags.None);
                    //texDataRect = _Textures[i * _TextureHorizontalDivide + j].LockRectangle(0, LockFlags.None);
                    byte[] texDataBytes = new byte[texDataRect.Data.Length];
                    int texelBytesPerPixel = texDataRect.Pitch / imgRect.Width;
                    int bytesOffset;
                    for(int m = 0; m < imgRect.Height; m++)
                    {
                        for (int n = 0; n < imgRect.Width; n++)
                        {
                            texelBytesPerPixel = texDataRect.Pitch / imgRect.Width;
                            bytesOffset = 0;
                            for (int p=0;p < imageInfo.BytesPerPixel; p++)
                            {
                                texDataBytes[m * texDataRect.Pitch + n * texelBytesPerPixel + p + bytesOffset] =
                                    pixels[m * imgRect.Width * imageInfo.BytesPerPixel + n * imageInfo.BytesPerPixel + p];
                            }
                            while (texelBytesPerPixel > imageInfo.BytesPerPixel)
                            {
                                texDataBytes[m * texDataRect.Pitch + n * texelBytesPerPixel + imageInfo.BytesPerPixel + (texelBytesPerPixel - imageInfo.BytesPerPixel - 1)] = 0xff;
                                texelBytesPerPixel--;
                                bytesOffset++;
                            }
                        }
                    }
                    texDataRect.Data.WriteRange(texDataBytes);
                    tmpTexture.UnlockRectangle(0);
                    //_Textures[i * _TextureHorizontalDivide + j].UnlockRectangle(0);
                    texDataRect.Data.Dispose();
                    _Device.UpdateTexture(tmpTexture, _Textures[i * _TextureHorizontalDivide + j]);
                    tmpTexture.Dispose();
                    tmpTexture = null;
                    texDataRect = null;
                    texDataBytes = null;
                }
            }

            return Result.Last;
        }
        private IntPtr CreateSurface(int imageWidth, int imageHeight)
        {
            int tempWidth = imageWidth;
            int tempHeight = imageHeight;
            double scale = 1.0;
            if (imageWidth > 2048)
            {
                scale = 2048.0 / imageWidth;
            }
            imageHeight = (int)(scale * imageHeight);
            if (imageHeight > 2048)
            {
                scale = scale * (2048.0 / imageHeight);
            }
            tempWidth = (int)(tempWidth * scale);
            tempHeight = (int)(tempHeight * scale);

            _Surface = Surface.CreateRenderTarget(_Device, tempWidth, tempHeight, Format.A8R8G8B8, MultisampleType.None, 0, true);
            if (Result.Last.IsFailure)
            {
                return IntPtr.Zero;
            }
            _Device.SetRenderTarget(0, _Surface);
            if (Result.Last.IsFailure)
            {
                return IntPtr.Zero;
            }
            else
            {
                return _Surface.ComPointer;
            }
        }
        private Result InitVertices()
        {
            CustomVertex[] vertices = new CustomVertex[4 * _TextureHorizontalDivide * _TextureVerticalDivide];
            float deltaX = 2.0f / _TextureHorizontalDivide;
            float deltaY = 2.0f / _TextureVerticalDivide;
            for(int i = 0; i < _TextureVerticalDivide; i++)
            {
                for(int j = 0; j < _TextureHorizontalDivide; j++)
                {
                    int rectCount = 4 * (i * _TextureHorizontalDivide + j);

                    float xLeft = -1.0f + j * deltaX;
                    float yTop = 1.0f - i * deltaY;
                    float xRight = xLeft + deltaX;
                    float yLower = yTop - deltaY;
                    if (j == _TextureHorizontalDivide - 1)
                    {
                        xRight = 1.0f;
                    }
                    if (i == _TextureVerticalDivide - 1)
                    {
                        yLower = -1.0f;
                    }

                    vertices[rectCount] = new CustomVertex() { Pos = new Vector3(xLeft, yTop, 0.0f), Tex = new Vector2(0.0f, 0.0f) };           // top left
                    vertices[rectCount + 1] = new CustomVertex() { Pos = new Vector3(xRight, yTop, 0.0f), Tex = new Vector2(1.0f, 0.0f) };      // top right
                    vertices[rectCount + 2] = new CustomVertex() { Pos = new Vector3(xLeft, yLower, 0.0f), Tex = new Vector2(0.0f, 1.0f) };     // lower left
                    vertices[rectCount + 3] = new CustomVertex() { Pos = new Vector3(xRight, yLower, 0.0f), Tex = new Vector2(1.0f, 1.0f) };    // lower right
                }
            }

            _VertexStride = System.Runtime.InteropServices.Marshal.SizeOf(vertices[0]);
            _VertexBuffer = new VertexBuffer(_Device, vertices.Length * _VertexStride, Usage.None, CUSTOMVERTEXFORMAT, Pool.Default);
            if (Result.Last.IsFailure)
            {
                return Result.Last;
            }
            _VertexBuffer.Lock(0, 0, LockFlags.None).WriteRange(vertices);
            if (Result.Last.IsFailure)
            {
                return Result.Last;
            }
            _VertexBuffer.Unlock();
            return Result.Last;
        }

       private void SetupMatrices(Vector3 eyePos)
        {
            Matrix matWorld = Matrix.Identity;

            Vector3 vecEye = eyePos;
            Vector3 vecLookAt = new Vector3(eyePos.X, eyePos.Y, 0.0f);
            Vector3 vecUp = new Vector3(0.0f, 1.0f, 0.0f);
            Matrix matView = Matrix.LookAtLH(vecEye, vecLookAt, vecUp);

            Matrix matProj = Matrix.PerspectiveFovLH((float)Math.PI / 2, 1.0f / 1.0f, 0.001f, 100.0f);

            _WorldViewProj = Matrix.Multiply(Matrix.Multiply(matWorld, matView), matProj);
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
