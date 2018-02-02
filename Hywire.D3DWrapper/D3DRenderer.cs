using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using SlimDX;
using SlimDX.Direct3D9;
using System.Drawing;
using System.Windows.Media.Imaging;
using System.IO;

namespace Hywire.D3DWrapper
{
    internal class D3DRenderer
    {
        #region Private Fields
        private Direct3D _Direct3D;
        private Device _Device;
        private Effect _Effect;
        private Texture _Texture;
        private Surface _Surface;
        private VertexBuffer _VertexBuffer;
        private int _VertexStride;
        private Matrix _WorldViewProj;

        private Texture _TexTopLeft;
        private Texture _TexTopRight;
        private Texture _TexLowerLeft;
        private Texture _TexLowerRight;
        private struct CustomVertex
        {
            public Vector3 Pos;
            public Vector2 Tex;
        }
        private const VertexFormat CUSTOMVERTEXFORMAT = VertexFormat.Position | VertexFormat.Texture1;
        #endregion Private Fields
        #region Public Properties
        public IntPtr SurfacePointer
        {
            get; private set;
        }
        #endregion Public Properties

        #region Public Functions
        public Result Initialize(WriteableBitmap image, IntPtr hWnd)
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
                AutoDepthStencilFormat = Format.D24S8,
                //Multisample = MultisampleType.TwoSamples,
                //MultisampleQuality = 0,
            };
            _Device = new Device(_Direct3D, 0, DeviceType.Hardware, hWnd,
                CreateFlags.HardwareVertexProcessing | CreateFlags.Multithreaded | CreateFlags.FpuPreserve, d3dpp);
            _Device.SetRenderState(RenderState.CullMode, Cull.None);
            _Device.SetRenderState(RenderState.Lighting, false);
            _Device.SetRenderState(RenderState.ZEnable, false);
            //_Device.SetRenderState(RenderState.MultisampleAntialias, true);

            SurfacePointer = CreateSurface(image.PixelWidth, image.PixelHeight);
            //SurfacePointer = CreateSurface((int)image.Width, (int)image.Height);

            CreateTextures(image);

            InitGeometry();

            string effectPath = Environment.CurrentDirectory + "\\DisplayShader.fx";
            _Effect = Effect.FromFile(_Device, effectPath, ShaderFlags.NoPreshader);

            image = null;

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
        public void Draw(ImageDisplayParameters displayParameters)
        {
            _Device.Clear(ClearFlags.Target, Color.Beige.ToArgb(), 1.0f, 0);
            if (_Device.BeginScene().IsSuccess)
            {
                SetupMatrices(displayParameters.ViewerPosition);
                _Device.SetStreamSource(0, _VertexBuffer, 0, _VertexStride);
                _Device.VertexFormat = CUSTOMVERTEXFORMAT;

                for (int i = 0; i < 4; i++)
                {
                    switch (i)
                    {
                        case 0:
                            _Effect.SetTexture(_Effect.GetParameter(null, "g_Texture"), _TexTopLeft);
                            break;
                        case 1:
                            _Effect.SetTexture(_Effect.GetParameter(null, "g_Texture"), _TexTopRight);
                            break;
                        case 2:
                            _Effect.SetTexture(_Effect.GetParameter(null, "g_Texture"), _TexLowerLeft);
                            break;
                        case 3:
                            _Effect.SetTexture(_Effect.GetParameter(null, "g_Texture"), _TexLowerRight);
                            break;
                    }
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
                    _Device.DrawPrimitives(PrimitiveType.TriangleStrip, i * 4, 2);
                    _Effect.EndPass();
                    _Effect.End();
                }

                _Device.EndScene();
            }
        }
        public void CleanUp()
        {
            //_Texture.Dispose();
            //_Surface.Dispose();
            //_Effect.Dispose();
            //_VertexBuffer.Dispose();
            //_Device.Dispose();
            //_Direct3D.Dispose();
            SafeRelease(_Texture);
            SafeRelease(_TexTopLeft);
            SafeRelease(_TexTopRight);
            SafeRelease(_TexLowerLeft);
            SafeRelease(_TexLowerRight);
            SafeRelease(_Surface);
            SafeRelease(_Effect);
            SafeRelease(_VertexBuffer);
            SafeRelease(_Device);
            SafeRelease(_Direct3D);
        }
        #endregion Public Functions

        #region Private Functions
        private Result CreateTextures(WriteableBitmap image)
        {
            int dataLength = image.BackBufferStride * image.PixelHeight;
            byte[] pixelData = new byte[dataLength];
            //image.CopyPixels(pixelData, image.BackBufferStride, 0);
            //_Texture = new Texture(_Device, image.PixelWidth, image.PixelHeight, 1, Usage.None, Format.L16, Pool.Managed);
            //DataRectangle texRect = _Texture.LockRectangle(0, LockFlags.None);
            //texRect.Data.WriteRange(pixelData);
            //_Texture.UnlockRectangle(0);

            int leftTexWidth = image.PixelWidth / 2;
            int rightTexWidth = image.PixelWidth - leftTexWidth + 1;
            int topTexHeight = image.PixelHeight / 2;
            int lowerTexHeight = image.PixelHeight - topTexHeight + 1;

            _TexTopLeft = new Texture(_Device, leftTexWidth, topTexHeight, 1, Usage.None, Format.L16, Pool.Managed);
            _TexTopRight = new Texture(_Device, rightTexWidth, topTexHeight, 1, Usage.None, Format.L16, Pool.Managed);
            _TexLowerLeft = new Texture(_Device, leftTexWidth, lowerTexHeight, 1, Usage.None, Format.L16, Pool.Managed);
            _TexLowerRight = new Texture(_Device, rightTexWidth, lowerTexHeight, 1, Usage.None, Format.L16, Pool.Managed);

            image.CopyPixels(new System.Windows.Int32Rect(leftTexWidth - 1, 0, rightTexWidth, topTexHeight), pixelData, rightTexWidth * 2, 0);
            DataRectangle texData = _TexTopRight.LockRectangle(0, LockFlags.None);
            byte[] tempBytes = new byte[texData.Data.Length];
            for (int i = 0; i < topTexHeight; i++)
            {
                for (int j = 0; j < rightTexWidth * 2; j++)
                {
                    tempBytes[i * texData.Pitch + j] = pixelData[i * rightTexWidth * 2 + j];
                }
            }
            texData.Data.WriteRange(tempBytes);
            _TexTopRight.UnlockRectangle(0);

            image.CopyPixels(new System.Windows.Int32Rect(0, 0, leftTexWidth, topTexHeight), pixelData, leftTexWidth * 2, 0);
            texData = _TexTopLeft.LockRectangle(0, LockFlags.None);
            tempBytes = new byte[texData.Data.Length];
            for (int i = 0; i < topTexHeight; i++)
            {
                for (int j = 0; j < leftTexWidth * 2; j++)
                {
                    tempBytes[i * texData.Pitch + j] = pixelData[i * leftTexWidth * 2 + j];
                }
            }
            texData.Data.WriteRange(tempBytes);
            _TexTopLeft.UnlockRectangle(0);

            image.CopyPixels(new System.Windows.Int32Rect(0, topTexHeight - 1, leftTexWidth, lowerTexHeight), pixelData, leftTexWidth * 2, 0);
            texData = _TexLowerLeft.LockRectangle(0, LockFlags.None);
            tempBytes = new byte[texData.Data.Length];
            for (int i = 0; i < lowerTexHeight; i++)
            {
                for (int j = 0; j < leftTexWidth * 2; j++)
                {
                    tempBytes[i * texData.Pitch + j] = pixelData[i * leftTexWidth * 2 + j];
                }
            }
            texData.Data.WriteRange(tempBytes);
            _TexLowerLeft.UnlockRectangle(0);

            image.CopyPixels(new System.Windows.Int32Rect(leftTexWidth - 1, topTexHeight - 1, rightTexWidth, lowerTexHeight), pixelData, rightTexWidth * 2, 0);
            texData = _TexLowerRight.LockRectangle(0, LockFlags.None);
            tempBytes = new byte[texData.Data.Length];
            for (int i = 0; i < lowerTexHeight; i++)
            {
                for (int j = 0; j < rightTexWidth * 2; j++)
                {
                    tempBytes[i * texData.Pitch + j] = pixelData[i * rightTexWidth * 2 + j];
                }
            }
            texData.Data.WriteRange(tempBytes);
            _TexLowerRight.UnlockRectangle(0);

            pixelData = null;
            texData = null;
            tempBytes = null;
            image = null;
            GC.Collect();

            return Result.Last;
        }
        private Result InitGeometry()
        {
            CustomVertex[] vertices = new CustomVertex[]
            {
                //new CustomVertex(){Pos=new Vector3(-1.0f, 1.0f,0.0f),Tex=new Vector2(0.0f,0.0f)},    // top left
                //new CustomVertex(){Pos=new Vector3( 1.0f, 1.0f,0.0f),Tex=new Vector2(1.0f,0.0f)},    // top right
                //new CustomVertex(){Pos=new Vector3(-1.0f,-1.0f,0.0f),Tex=new Vector2(0.0f,1.0f)},    // lower left
                //new CustomVertex(){Pos=new Vector3( 1.0f,-1.0f,0.0f),Tex=new Vector2(1.0f,1.0f)},    // lower right

                new CustomVertex() {Pos=new Vector3(-1.0f, 1.0f, 0.0f ), Tex=new Vector2(0.0f, 0.0f) },     // top left
                new CustomVertex() {Pos=new Vector3( 0.0f, 1.0f, 0.0f ), Tex=new Vector2(1.0f, 0.0f) },      // top right
                new CustomVertex() {Pos=new Vector3(-1.0f, 0.0f, 0.0f ), Tex=new Vector2(0.0f, 1.0f) },    // lower left
                new CustomVertex() {Pos=new Vector3( 0.0f, 0.0f, 0.0f ), Tex=new Vector2(1.0f, 1.0f) },     // lower right

                new CustomVertex() {Pos=new Vector3( 0.0f, 1.0f, 0.0f ), Tex=new Vector2(0.0f, 0.0f) },     // top left
                new CustomVertex() {Pos=new Vector3( 1.0f, 1.0f, 0.0f ), Tex=new Vector2(1.0f, 0.0f) },      // top right
                new CustomVertex() {Pos=new Vector3( 0.0f, 0.0f, 0.0f ), Tex=new Vector2(0.0f, 1.0f) },    // lower left
                new CustomVertex() {Pos=new Vector3( 1.0f, 0.0f, 0.0f ), Tex=new Vector2(1.0f, 1.0f) },     // lower right

                new CustomVertex() {Pos=new Vector3(-1.0f, 0.0f, 0.0f ), Tex=new Vector2(0.0f, 0.0f) },     // top left
                new CustomVertex() {Pos=new Vector3( 0.0f, 0.0f, 0.0f ), Tex=new Vector2(1.0f, 0.0f) },      // top right
                new CustomVertex() {Pos=new Vector3(-1.0f,-1.0f, 0.0f ), Tex=new Vector2(0.0f, 1.0f) },    // lower left
                new CustomVertex() {Pos=new Vector3( 0.0f,-1.0f, 0.0f ), Tex=new Vector2(1.0f, 1.0f) },     // lower right

                new CustomVertex() {Pos=new Vector3( 0.0f, 0.0f, 0.0f ), Tex=new Vector2(0.0f, 0.0f) },     // top left
                new CustomVertex() {Pos=new Vector3( 1.0f, 0.0f, 0.0f ), Tex=new Vector2(1.0f, 0.0f) },      // top right
                new CustomVertex() {Pos=new Vector3( 0.0f,-1.0f, 0.0f ), Tex=new Vector2(0.0f, 1.0f) },    // lower left
                new CustomVertex() {Pos=new Vector3( 1.0f,-1.0f, 0.0f ), Tex=new Vector2(1.0f, 1.0f) },     // lower right
            };
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
