using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using SlimDX;
using SlimDX.Direct3D9;
using System.Drawing;

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
        private struct CustomVertex
        {
            public Vector4 Pos;
            public Vector2 Tex;
        }
        private const VertexFormat CUSTOMVERTEXFORMAT = VertexFormat.PositionW | VertexFormat.Texture1;
        #endregion Private Fields
        #region Public Properties
        public IntPtr SurfacePointer
        {
            get; private set;
        }
        #endregion Public Properties

        #region Public Functions
        public Result Initialize(string imagePath, int imageWidth, int imageHeight, IntPtr hWnd)
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
                EnableAutoDepthStencil = true,
                AutoDepthStencilFormat = Format.D24X8,
                //Multisample = MultisampleType.TwoSamples,
                //MultisampleQuality = 0,
            };
            _Device = new Device(_Direct3D, 0, DeviceType.Hardware, hWnd,
                CreateFlags.SoftwareVertexProcessing | CreateFlags.Multithreaded | CreateFlags.FpuPreserve, d3dpp);
            _Device.SetRenderState(RenderState.CullMode, Cull.None);
            _Device.SetRenderState(RenderState.Lighting, false);
            _Device.SetRenderState(RenderState.ZEnable, true);
            //_Device.SetRenderState(RenderState.MultisampleAntialias, true);

            SurfacePointer = CreateSurface(imageWidth, imageHeight);

            //_Texture = Texture.FromStream(_Device, image.StreamSource, image.PixelWidth, image.PixelHeight, 1, Usage.None, Format.A16B16G16R16, Pool.SystemMemory, Filter.Linear, Filter.Linear, 0);
            //_Texture = Texture.FromMemory(_Device, image.;
            //_Texture = new Texture(_Device, image.Width, image.Height, 1, Usage.None, Format.A16B16G16R16, Pool.Default);
            //TextureShader textureShader = TextureShader.
            //_Texture.Fill()
            _Texture = Texture.FromFile(_Device, imagePath, imageWidth, imageHeight, 0, Usage.None,
                Format.Unknown, Pool.Managed, Filter.Linear, Filter.Linear, 0);

            InitGeometry();

            //string effectPath = Environment.CurrentDirectory + "\\DisplayShader.fx";
            string effectPath = string.Format(@"E:\Users\paul\Documents\Visual Studio 2015\Projects\Works\DirectXStudy\D3DImageViewer\Hywire.D3DWrapper\DisplayShader.fx");
            //string effectPath = string.Format(@"E:\Users\paul\Documents\Visual Studio 2015\Projects\Works\ImageDisplayer\Hywire.WPFD3DImageTest\PixelShader.fx");
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
            _Device.Clear(ClearFlags.Target | ClearFlags.ZBuffer, Color.Beige.ToArgb(), 1.0f, 0);
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
        private Result InitGeometry()
        {
            CustomVertex[] vertices = new CustomVertex[4]
            {
                new CustomVertex(){Pos=new Vector4(-1.0f, 1.0f,0.0f,1.0f),Tex=new Vector2(0.0f,0.0f)},    // top left
                new CustomVertex(){Pos=new Vector4( 1.0f, 1.0f,0.0f,1.0f),Tex=new Vector2(1.0f,0.0f)},    // top right
                new CustomVertex(){Pos=new Vector4(-1.0f,-1.0f,0.0f,1.0f),Tex=new Vector2(0.0f,1.0f)},    // lower left
                new CustomVertex(){Pos=new Vector4( 1.0f,-1.0f,0.0f,1.0f),Tex=new Vector2(1.0f,1.0f)},    // lower right
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
        private void SetupMatrices()
        {
            Matrix matWorld = Matrix.Identity;

            Vector3 vecEye = new Vector3(0.0f, 0.0f, -2.0f);
            Vector3 vecLookAt = new Vector3(0.0f, 0.0f, 0.0f);
            Vector3 vecUp = new Vector3(0.0f, 1.0f, 0.0f);
            Matrix matView = Matrix.LookAtLH(vecEye, vecLookAt, vecUp);

            Matrix matProj = Matrix.PerspectiveFovLH((float)Math.PI / 4, 1.0f / 1.0f, 0.1f, 100.0f);

            _WorldViewProj = Matrix.Multiply(Matrix.Multiply(matWorld, matView), matProj);
        }
        private void SafeRelease(ComObject obj)
        {
            if (obj != null)
            {
                obj.Dispose();
            }
        }
        #endregion Private Functions
    }
}
