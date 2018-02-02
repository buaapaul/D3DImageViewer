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
        public Result Initialize(BitmapImage image, int imageWidth, int imageHeight, IntPtr hWnd)
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

            SurfacePointer = CreateSurface(imageWidth, imageHeight);
            //SurfacePointer = CreateSurface((int)image.Width, (int)image.Height);

            //_Texture = Texture.FromPointer(_Device, imagePath, imageWidth, imageHeight, 0, Usage.None,
            //    Format.A16B16G16R16, Pool.Managed, Filter.Point, Filter.Point, 0);

            ////System.IO.MemoryStream imageStream = new System.IO.MemoryStream();
            ////image.Save(imageStream, image.RawFormat);
            //System.Drawing.Imaging.BitmapData bmpData = image.LockBits(new Rectangle(0, 0, image.Width, image.Height), 
            //    System.Drawing.Imaging.ImageLockMode.ReadOnly, image.PixelFormat);
            //var length = bmpData.Stride * bmpData.Height;
            //byte[] imageBytes = new byte[length];
            //System.Runtime.InteropServices.Marshal.Copy(bmpData.Scan0, imageBytes, 0, length);
            //_Texture = new Texture(_Device, image.Width, image.Height,0, Usage.Dynamic, Format.A16B16G16R16, Pool.Default);
            //var texData = _Texture.LockRectangle(0, LockFlags.None);
            //texData.Data.Write(imageBytes, 0, length);
            //_Texture.UnlockRectangle(0);
            //imageBytes = null;

            //WriteableBitmap wrImg = new WriteableBitmap(image);
            //int length = wrImg.BackBufferStride * wrImg.PixelHeight;
            //byte[] imgData = new byte[length];
            //wrImg.CopyPixels(imgData, wrImg.BackBufferStride, 0);
            //byte[] texData = new byte[imgData.Length * 4];
            //for (int i = 0; i < imgData.Length/2; i++)
            //{
            //    texData[i * 8] = 255;
            //    texData[i * 8 + 1] = 255;
            //    texData[i * 8 + 2] = imgData[i];
            //    texData[i * 8 + 3] = imgData[i + 1];
            //    texData[i * 8 + 4] = imgData[i];
            //    texData[i * 8 + 5] = imgData[i + 1];
            //    texData[i * 8 + 6] = imgData[i];
            //    texData[i * 8 + 7] = imgData[i + 1];
            //}
            //_Texture = new Texture(_Device, image.PixelWidth, image.PixelHeight, 1, Usage.Dynamic, Format.A16B16G16R16, Pool.Default);
            //var texWriting = _Texture.LockRectangle(0, LockFlags.None);
            //for (int i = 0; i < image.PixelHeight; i++)
            //{
            //    texWriting.Data.Write(texData, wrImg.BackBufferStride * i, 4 * wrImg.BackBufferStride);
            //}
            //_Texture.UnlockRectangle(0);

            byte[] bytearray = null;
            Stream smarket = image.StreamSource;
            if (smarket != null && smarket.Length > 0)
            {
                //很重要，因为position经常位于stream的末尾，导致下面读取到的长度为0。
                smarket.Position = 0;
                using (BinaryReader br = new BinaryReader(smarket))
                {
                    bytearray = br.ReadBytes((int)smarket.Length);
                }
            }
            _Texture = Texture.FromMemory(_Device, bytearray, image.PixelWidth, image.PixelHeight, 1, Usage.None, Format.A16B16G16R16, Pool.Default, Filter.Point, Filter.Point, 0);
            bytearray = null;

            InitGeometry();

            string effectPath = Environment.CurrentDirectory + "\\DisplayShader.fx";
            _Effect = Effect.FromFile(_Device, effectPath, ShaderFlags.NoPreshader);
            _Effect.SetTexture(_Effect.GetParameter(null, "g_Texture"), _Texture);

            return Result.Last;
        }
        private IntPtr CreateSurface(int imageWidth, int imageHeight)
        {
            _Surface = Surface.CreateRenderTarget(_Device, imageWidth, imageHeight, Format.A8R8G8B8, MultisampleType.None, 0, true);
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
                SetupMatrices();
                _Device.SetTexture(0, _Texture);
                _Device.SetStreamSource(0, _VertexBuffer, 0, _VertexStride);
                _Device.VertexFormat = CUSTOMVERTEXFORMAT;
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
                _Device.DrawPrimitives(PrimitiveType.TriangleStrip, 0, 2);
                _Effect.EndPass();
                _Effect.End();
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
            SafeRelease(_Surface);
            SafeRelease(_Effect);
            SafeRelease(_VertexBuffer);
            SafeRelease(_Device);
            SafeRelease(_Direct3D);
        }
        #endregion Public Functions

        #region Private Functions
        private Result CreateTextures(BitmapImage image)
        {
            int leftTexWidth = image.PixelWidth / 2;
            int rightTexWidth = image.PixelWidth - leftTexWidth;
            int topTexHeight = image.PixelHeight / 2;
            int lowerTexHeight = image.PixelHeight - topTexHeight;

            _TexTopLeft = new Texture(_Device, leftTexWidth, topTexHeight, 1, Usage.None, Format.A16B16G16R16, Pool.Managed);
            _TexTopRight = new Texture(_Device, rightTexWidth, topTexHeight, 1, Usage.None, Format.A16B16G16R16, Pool.Managed);
            _TexLowerLeft = new Texture(_Device, leftTexWidth, lowerTexHeight, 1, Usage.None, Format.A16B16G16R16, Pool.Managed);
            _TexLowerRight = new Texture(_Device, rightTexWidth, lowerTexHeight, 1, Usage.None, Format.A16B16G16R16, Pool.Managed);

            return Result.Last;
        }
        private Result InitGeometry()
        {
            CustomVertex[] vertices = new CustomVertex[4]
            {
                new CustomVertex(){Pos=new Vector3(-1.0f, 1.0f,0.0f),Tex=new Vector2(0.0f,0.0f)},    // top left
                new CustomVertex(){Pos=new Vector3( 1.0f, 1.0f,0.0f),Tex=new Vector2(1.0f,0.0f)},    // top right
                new CustomVertex(){Pos=new Vector3(-1.0f,-1.0f,0.0f),Tex=new Vector2(0.0f,1.0f)},    // lower left
                new CustomVertex(){Pos=new Vector3( 1.0f,-1.0f,0.0f),Tex=new Vector2(1.0f,1.0f)},    // lower right
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

        private float eyePos = -1.0f;
        private int dir = 1;
        private void SetupMatrices()
        {
            Matrix matWorld = Matrix.Identity;

            Vector3 vecEye = new Vector3(0.0f, 0.0f, eyePos);
            //eyePos -= 0.01f*dir;
            if (eyePos < -1.5f)
            {
                dir = -1;
            }
            else if (eyePos > -0.5f)
            {
                dir = 1;
            }
            Vector3 vecLookAt = new Vector3(0.0f, 0.0f, 0.0f);
            Vector3 vecUp = new Vector3(0.0f, 1.0f, 0.0f);
            Matrix matView = Matrix.LookAtLH(vecEye, vecLookAt, vecUp);

            Matrix matProj = Matrix.PerspectiveFovLH((float)Math.PI / 2, 1.0f / 1.0f, 0.1f, 100.0f);

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
