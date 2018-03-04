using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using SlimDX;
using SlimDX.Direct3D11;
using SlimDX.D3DCompiler;
using System.IO;
using System.Reflection;
using System.Drawing;

namespace Hywire.D3DWrapper
{
    internal class DirectX11Wrapper
    {
        #region Private Fields
        private Device _Device;
        private ShaderResourceView[] _TextureViews;
        private int _TextureHorizontalDivide = 1;
        private int _TextureVerticalDivide = 1;
        private SlimDX.Direct3D11.Buffer _VertexBuffer;
        private SlimDX.Direct3D11.Buffer _IndexBuffer;
        private Effect _Effect;
        private EffectTechnique _EffectTechnique;
        private EffectResourceVariable _EffectTexture;
        private EffectMatrixVariable _EffectMatrix;
        private int _RenderingWidth = 100;
        private int _RenderingHeight = 100;
        private struct CustomVertex
        {
            public Vector3 Pos;
            public Vector2 Tex;
        }
        #endregion Private Fields

        #region Public Properties
        public Texture2D RenderTargetTexture
        {
            get; private set;
        }
        #endregion Public Properties

        #region Public Functions
        public void Initialize(ImageInfo imageInfo, IntPtr hWnd)
        {
            int tempWidth = imageInfo.PixelWidth;
            int tempHeight = imageInfo.PixelHeight;
            double scale = 1.0;
            if (tempWidth > 2048)
            {
                scale = 2048.0 / tempWidth;
            }
            tempHeight = (int)(scale * tempHeight);
            if (tempHeight > 2048)
            {
                scale = scale * (2048.0 / tempHeight);
            }
            _RenderingWidth = (int)(imageInfo.PixelWidth * scale);
            _RenderingHeight = (int)(imageInfo.PixelHeight * scale);

            //SlimDX.DXGI.SwapChainDescription swapChainDesc = new SlimDX.DXGI.SwapChainDescription()
            //{
            //    BufferCount = 1,
            //    ModeDescription = new SlimDX.DXGI.ModeDescription()
            //    {
            //        Width = _RenderingWidth,
            //        Height = _RenderingHeight,
            //        Format = SlimDX.DXGI.Format.R8G8B8A8_UNorm,
            //        RefreshRate = new Rational(60, 1),
            //    },
            //    Usage = SlimDX.DXGI.Usage.RenderTargetOutput,
            //    OutputHandle = hWnd,
            //    SampleDescription = new SlimDX.DXGI.SampleDescription { Count = 1, Quality = 0 },
            //    IsWindowed = true,
            //};
            //Device.CreateWithSwapChain(DriverType.Hardware, DeviceCreationFlags.None, new FeatureLevel[] { FeatureLevel.Level_11_0 }, swapChainDesc, out _Device, out _SwapChain);

            _Device = new Device(DriverType.Hardware, DeviceCreationFlags.None, new FeatureLevel[] { FeatureLevel.Level_11_0 });
            Viewport viewPort = new Viewport
            {
                Width = _RenderingWidth,
                Height = _RenderingHeight,
                MaxZ = 1.0f,
                MinZ = 0.0f,
                X = 0,
                Y = 0,
            };
            _Device.ImmediateContext.Rasterizer.SetViewports(viewPort);
            //_Device.ImmediateContext.Rasterizer.State = RasterizerState.FromDescription(_Device, new RasterizerStateDescription
            //{
            //    CullMode = CullMode.None,
            //    FillMode = FillMode.Solid,
            //});

            CreateTextures(imageInfo);

            InitEffect();

            InitVertices();

            InitRenderTarget();
        }
        public void Draw(ImageDisplayParameterStruct displayParameters)
        {
            RenderTargetView renderTargetView = new RenderTargetView(_Device, RenderTargetTexture);
            _Device.ImmediateContext.OutputMerger.SetTargets(renderTargetView);

            _Device.ImmediateContext.ClearRenderTargetView(renderTargetView, new Color4(Color.Beige.ToArgb()));

            for (int i = 0; i < _TextureVerticalDivide; i++)
            {
                for (int j = 0; j < _TextureHorizontalDivide; j++)
                {
                    SetupDisplayParameters(displayParameters);
                    _EffectTexture.SetResource(_TextureViews[i * _TextureHorizontalDivide + j]);
                    _EffectTechnique.GetPassByIndex(0).Apply(_Device.ImmediateContext);
                    _Device.ImmediateContext.DrawIndexed(6, (i * _TextureHorizontalDivide + j) * 6, 0);
                }
            }
            renderTargetView.Dispose();
            renderTargetView = null;
            _Device.ImmediateContext.Flush();
        }
        public void CleanUp()
        {
            if (_TextureViews != null)
            {
                for(int i=0;i< _TextureViews.Length; i++)
                {
                    SafeRelease(_TextureViews[i]);
                }
            }
            SafeRelease(RenderTargetTexture);
            SafeRelease(_Effect);
            SafeRelease(_VertexBuffer);
            SafeRelease(_IndexBuffer);
            SafeRelease(_Device);
        }
        #endregion Public Functions

        #region Private Functions
        private void CreateRenderTarget()
        {
            Texture2DDescription texDesc = new Texture2DDescription
            {
                Width = _RenderingWidth,
                Height = _RenderingHeight,
                MipLevels = 1,
                ArraySize = 1,
                Format = SlimDX.DXGI.Format.B8G8R8A8_UNorm,
                SampleDescription = new SlimDX.DXGI.SampleDescription { Count = 1, Quality = 0 },
                Usage = ResourceUsage.Default,
                BindFlags = BindFlags.RenderTarget | BindFlags.ShaderResource,
                CpuAccessFlags = CpuAccessFlags.None,
                OptionFlags = ResourceOptionFlags.Shared,
            };
            RenderTargetTexture = new Texture2D(_Device, texDesc);
        }
        private void CreateTextures(ImageInfo imageInfo)
        {
            int maxTexDimension = 8192;
            if (imageInfo.PixelWidth > maxTexDimension)
            {
                _TextureHorizontalDivide = (int)(imageInfo.PixelWidth / (double)maxTexDimension) + 1;
            }
            else { _TextureHorizontalDivide = 1; }
            if (imageInfo.PixelHeight > maxTexDimension)
            {
                _TextureVerticalDivide = (int)(imageInfo.PixelHeight / (double)maxTexDimension) + 1;
            }
            else { _TextureVerticalDivide = 1; }
            if (_TextureViews != null)
            {
                for (int i = 0; i < _TextureViews.Length; i++)
                {
                    SafeRelease(_TextureViews[i]);
                }
            }
            _TextureViews = new ShaderResourceView[_TextureHorizontalDivide * _TextureVerticalDivide];
            int texAverageWidth = imageInfo.PixelWidth / _TextureHorizontalDivide;
            int texAverageHeight = imageInfo.PixelHeight / _TextureVerticalDivide;
            System.Windows.Int32Rect imgRect = new System.Windows.Int32Rect();
            byte[] pixels = new byte[imageInfo.BackBufferStride * imageInfo.PixelHeight];
            SlimDX.DXGI.Format format = ConvertToDXGIFormat(imageInfo.Image.Format);

            for (int i = 0; i < _TextureVerticalDivide; i++)
            {
                for (int j = 0; j < _TextureHorizontalDivide; j++)
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
                    imageInfo.Image.CopyPixels(imgRect, pixels, imgRect.Width * imageInfo.BytesPerPixel, 0);
                    Texture2DDescription texDesc = new Texture2DDescription
                    {
                        ArraySize = 1,
                        BindFlags = BindFlags.ShaderResource,
                        CpuAccessFlags = CpuAccessFlags.Write,
                        Format = format,
                        Height = imgRect.Height,
                        Width = imgRect.Width,
                        MipLevels = 1,
                        OptionFlags = ResourceOptionFlags.None,
                        SampleDescription = new SlimDX.DXGI.SampleDescription { Count = 1, Quality = 0 },
                        Usage = ResourceUsage.Dynamic,
                    };
                    Texture2D tex = new Texture2D(_Device, texDesc);
                    SlimDX.DXGI.Surface surface = tex.AsSurface();
                    DataRectangle dataRec = surface.Map(SlimDX.DXGI.MapFlags.Write | SlimDX.DXGI.MapFlags.Discard);
                    byte[] texDataBytes = new byte[dataRec.Data.Length];
                    int texelBytesPerPixel = dataRec.Pitch / imgRect.Width;
                    int bytesOffset;
                    for (int m = 0; m < imgRect.Height; m++)
                    {
                        for (int n = 0; n < imgRect.Width; n++)
                        {
                            texelBytesPerPixel = dataRec.Pitch / imgRect.Width;
                            bytesOffset = 0;
                            for (int p = 0; p < imageInfo.BytesPerPixel; p++)
                            {
                                texDataBytes[m * dataRec.Pitch + n * texelBytesPerPixel + p + bytesOffset] =
                                    pixels[m * imgRect.Width * imageInfo.BytesPerPixel + n * imageInfo.BytesPerPixel + p];
                            }
                            bytesOffset = texelBytesPerPixel;
                            while (bytesOffset > imageInfo.BytesPerPixel)
                            {
                                texDataBytes[m * dataRec.Pitch + n * texelBytesPerPixel + imageInfo.BytesPerPixel + (bytesOffset - imageInfo.BytesPerPixel - 1)] = 0xff;
                                bytesOffset--;
                            }
                        }
                    }
                    //dataRec.Data.WriteRange(texDataBytes);
                    for (int line = 0; line < imgRect.Height; line++)
                    {
                        dataRec.Data.Position = dataRec.Pitch * line;
                        dataRec.Data.WriteRange(texDataBytes, line * dataRec.Pitch, dataRec.Pitch);
                    }

                    tex.AsSurface().Unmap();
                    ShaderResourceViewDescription srvDesc = new ShaderResourceViewDescription
                    {
                        Format = tex.Description.Format,
                        Dimension = ShaderResourceViewDimension.Texture2D,
                        MipLevels = tex.Description.MipLevels,
                        MostDetailedMip = 0,
                    };
                    _TextureViews[i * _TextureHorizontalDivide + j] = new ShaderResourceView(_Device, tex, srvDesc);
                    tex.Dispose();
                    surface.Dispose();
                    dataRec.Data.Dispose();
                    texDataBytes = null;
                }
            }


            //int stride = imageInfo.Image.PixelWidth * imageInfo.Image.Format.BitsPerPixel / 8;
            //byte[] imgData = new byte[imageInfo.Image.PixelHeight * stride];
            //imageInfo.Image.CopyPixels(imgData, stride, 0);
            //Texture2DDescription texDesc = new Texture2DDescription
            //{
            //    ArraySize = 1,
            //    BindFlags = BindFlags.ShaderResource,
            //    CpuAccessFlags = CpuAccessFlags.Write,
            //    Format = SlimDX.DXGI.Format.R16_UNorm,
            //    Height = imageInfo.Image.PixelHeight,
            //    Width = imageInfo.Image.PixelWidth,
            //    MipLevels = 1,
            //    OptionFlags = ResourceOptionFlags.None,
            //    SampleDescription = new SlimDX.DXGI.SampleDescription { Count = 1, Quality = 0 },
            //    Usage = ResourceUsage.Dynamic,
            //};
            //Texture2D tex = new Texture2D(_Device, texDesc);
            //SlimDX.DXGI.Surface surface = tex.AsSurface();
            //DataRectangle dataRec = surface.Map(SlimDX.DXGI.MapFlags.Write | SlimDX.DXGI.MapFlags.Discard);
            //for (int i = 0; i < imageInfo.Image.PixelHeight; i++)
            //{
            //    dataRec.Data.Position = dataRec.Pitch * i;
            //    dataRec.Data.WriteRange(imgData, i * stride, stride);
            //}
            //tex.AsSurface().Unmap();
            //ShaderResourceViewDescription srvDesc = new ShaderResourceViewDescription
            //{
            //    Format = tex.Description.Format,
            //    Dimension = ShaderResourceViewDimension.Texture2D,
            //    MipLevels = tex.Description.MipLevels,
            //    MostDetailedMip = 0,
            //};
            //_TextureView = new ShaderResourceView(_Device, tex, srvDesc);

            //tex.Dispose();
            //surface.Dispose();
            //tex = null;
            //surface = null;
        }
        private void InitVertices()
        {
            // Create vertex buffer
            CustomVertex[] vertices = new CustomVertex[4 * _TextureHorizontalDivide * _TextureVerticalDivide];
            float deltaX = 2.0f / _TextureHorizontalDivide;
            float deltaY = 2.0f / _TextureVerticalDivide;
            for (int i = 0; i < _TextureVerticalDivide; i++)
            {
                for (int j = 0; j < _TextureHorizontalDivide; j++)
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
            DataStream vertexData = new DataStream(vertices, true, false);
            int stride = System.Runtime.InteropServices.Marshal.SizeOf(vertices[0]);
            BufferDescription bufferDesc = new BufferDescription
            {
                Usage = ResourceUsage.Default,
                SizeInBytes = vertices.Length * stride,
                BindFlags = BindFlags.VertexBuffer,
                CpuAccessFlags = CpuAccessFlags.None,
            };
            _VertexBuffer = new SlimDX.Direct3D11.Buffer(_Device, vertexData, bufferDesc);

            // Create index buffer
            int[] indices = new int[6 * _TextureHorizontalDivide * _TextureVerticalDivide];
            for(int i = 0; i < _TextureVerticalDivide; i++)
            {
                for(int j = 0; j < _TextureHorizontalDivide; j++)
                {
                    int baseOffset = (i * 6 * _TextureHorizontalDivide) + 6 * j;
                    int baseIndex = (i * 4 * _TextureHorizontalDivide) + 4 * j;
                    indices[baseOffset + 0] = baseIndex + 0;
                    indices[baseOffset + 1] = baseIndex + 1;
                    indices[baseOffset + 2] = baseIndex + 2;
                    indices[baseOffset + 3] = baseIndex + 1;
                    indices[baseOffset + 4] = baseIndex + 3;
                    indices[baseOffset + 5] = baseIndex + 2;
                }
            }
            bufferDesc.SizeInBytes = sizeof(int) * indices.Length;
            bufferDesc.BindFlags = BindFlags.IndexBuffer;
            _IndexBuffer = new SlimDX.Direct3D11.Buffer(_Device, new DataStream(indices, false, false), bufferDesc);

            // Set vertex buffer
            _Device.ImmediateContext.InputAssembler.SetVertexBuffers(0, new VertexBufferBinding(_VertexBuffer, stride, 0));
            // Set index buffer
            _Device.ImmediateContext.InputAssembler.SetIndexBuffer(_IndexBuffer, SlimDX.DXGI.Format.R32_UInt, 0);
            // Set primitive topology
            _Device.ImmediateContext.InputAssembler.PrimitiveTopology = PrimitiveTopology.TriangleList;

            //CustomVertex[] vertices = new CustomVertex[4]
            //{
            //    new CustomVertex() {Pos=new Vector3(-1.0f, 1.0f, 0.0f ), Tex=new Vector2(0.0f, 0.0f) },     // top left
            //    new CustomVertex() {Pos=new Vector3( 1.0f, 1.0f, 0.0f ), Tex=new Vector2(1.0f, 0.0f) },      // top right
            //    new CustomVertex() {Pos=new Vector3(-1.0f,-1.0f, 0.0f ), Tex=new Vector2(0.0f, 1.0f) },    // lower left
            //    new CustomVertex() {Pos=new Vector3( 1.0f,-1.0f, 0.0f ), Tex=new Vector2(1.0f, 1.0f) },     // lower right
            //};
            //DataStream vertexData = new DataStream(vertices, true, false);
            //int stride = System.Runtime.InteropServices.Marshal.SizeOf(vertices[0]);
            //BufferDescription bufferDesc = new BufferDescription
            //{
            //    Usage = ResourceUsage.Default,
            //    SizeInBytes = vertices.Length * stride,
            //    BindFlags = BindFlags.VertexBuffer,
            //    CpuAccessFlags = CpuAccessFlags.None,
            //};
            //_VertexBuffer = new SlimDX.Direct3D11.Buffer(_Device, vertexData, bufferDesc);

            //// Create index buffer
            //int[] indices = new int[]
            //{
            //    0,1,2,
            //    1,3,2,
            //};
            //bufferDesc.SizeInBytes = sizeof(int) * indices.Length;
            //bufferDesc.BindFlags = BindFlags.IndexBuffer;
            //_IndexBuffer = new SlimDX.Direct3D11.Buffer(_Device, new DataStream(indices, false, false), bufferDesc);

            //// Set vertex buffer
            //_Device.ImmediateContext.InputAssembler.SetVertexBuffers(0, new VertexBufferBinding(_VertexBuffer, stride, 0));
            //// Set index buffer
            //_Device.ImmediateContext.InputAssembler.SetIndexBuffer(_IndexBuffer, SlimDX.DXGI.Format.R32_UInt, 0);
            //// Set primitive topology
            //_Device.ImmediateContext.InputAssembler.PrimitiveTopology = PrimitiveTopology.TriangleList;
        }
        private void InitEffect()
        {
            //Stream effectStream = Assembly.GetExecutingAssembly().GetManifestResourceStream("Hywire.D3DWrapper.DX11Shader.fx");
            //byte[] effectBytes = new byte[effectStream.Length];
            //effectStream.Read(effectBytes, 0, effectBytes.Length);
            //effectStream.Dispose();
            //effectStream = null;
            //ShaderBytecode shaderByteCode = ShaderBytecode.Compile(effectBytes, "fx_5_0");
            string effectFilePath = @"E:\Users\paul\Documents\Visual Studio 2015\Projects\DirectX_Study\Works\Hywire.ImageViewer\Hywire.D3DWrapper\DX11Shader.fx";
            effectFilePath = Environment.CurrentDirectory + "\\DX11Shader.fx";
            ShaderBytecode shaderByteCode = ShaderBytecode.CompileFromFile(effectFilePath, "fx_5_0");
            _Effect = new Effect(_Device, shaderByteCode);
            shaderByteCode.Dispose();

            _EffectTechnique = _Effect.GetTechniqueByName("Render");
            _EffectTexture = _Effect.GetVariableByName("tex2D").AsResource();
            _EffectMatrix = _Effect.GetVariableByName("WorldViewProj").AsMatrix();

            // Define the input layout
            InputElement[] inputElements = new InputElement[2]
            {
                new InputElement("POSITION",0, SlimDX.DXGI.Format.R32G32B32_Float, 0,0, InputClassification.PerVertexData, 0),
                new InputElement("TEXCOORD",0, SlimDX.DXGI.Format.R32G32_Float, 12,0, InputClassification.PerVertexData, 0),
            };
            EffectPassDescription passDesc = _EffectTechnique.GetPassByIndex(0).Description;
            InputLayout layOut = new InputLayout(_Device, passDesc.Signature, inputElements);
            // Set the input layout
            _Device.ImmediateContext.InputAssembler.InputLayout = layOut;
            layOut.Dispose();
        }
        private void InitRenderTarget()
        {
            Texture2DDescription texDesc = new Texture2DDescription
            {
                Width = _RenderingWidth,
                Height = _RenderingHeight,
                MipLevels = 1,
                ArraySize = 1,
                Format = SlimDX.DXGI.Format.B8G8R8A8_UNorm,
                SampleDescription = new SlimDX.DXGI.SampleDescription { Count = 1, Quality = 0 },
                Usage = ResourceUsage.Default,
                BindFlags = BindFlags.RenderTarget | BindFlags.ShaderResource,
                CpuAccessFlags = CpuAccessFlags.None,
                OptionFlags = ResourceOptionFlags.Shared,
            };
            RenderTargetTexture = new Texture2D(_Device, texDesc);
        }
        private void SetupMatrices(Vector3 eyePos)
        {
            Matrix matWorld = Matrix.Identity;

            Vector3 vecEye = eyePos;
            Vector3 vecLookAt = new Vector3(eyePos.X, eyePos.Y, 0.0f);
            Vector3 vecUp = new Vector3(0.0f, 1.0f, 0.0f);
            Matrix matView = Matrix.LookAtLH(vecEye, vecLookAt, vecUp);

            Matrix matProj = Matrix.PerspectiveFovLH((float)Math.PI / 2, 1.0f / 1.0f, 0.001f, 100.0f);

            Matrix worldViewProj = Matrix.Multiply(Matrix.Multiply(matWorld, matView), matProj);

            _EffectMatrix.SetMatrix(worldViewProj);
        }
        private void SetupDisplayParameters(ImageDisplayParameterStruct displayParameters)
        {
            SetupMatrices(displayParameters.ViewerPosition);

            DataStream dataStream = new DataStream(new float[] { displayParameters.DisplayLimitHigh }, true, false);
            _Effect.GetVariableByName("g_DisplayRangeH").SetRawValue(dataStream, 4);
            dataStream.Dispose();
            dataStream = new DataStream(new float[] { displayParameters.DisplayLimitLow }, true, false);
            _Effect.GetVariableByName("g_DisplayRangeL").SetRawValue(dataStream, 4);
            dataStream.Dispose();
            dataStream = new DataStream(new int[] { displayParameters.RedChannelMap }, true, false);
            _Effect.GetVariableByName("g_RedChannelMap").SetRawValue(dataStream, 4);
            dataStream.Dispose();
            dataStream = new DataStream(new int[] { displayParameters.GreenChannelMap }, true, false);
            _Effect.GetVariableByName("g_GreenChannelMap").SetRawValue(dataStream, 4);
            dataStream.Dispose();
            dataStream = new DataStream(new int[] { displayParameters.BlueChannelMap }, true, false);
            _Effect.GetVariableByName("g_BlueChannelMap").SetRawValue(dataStream, 4);
            dataStream.Dispose();
            dataStream = new DataStream(new int[] { displayParameters.AlphaChannelMap }, true, false);
            _Effect.GetVariableByName("g_AlphaChannelMap").SetRawValue(dataStream, 4);
            dataStream.Dispose();
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
        private SlimDX.DXGI.Format ConvertToDXGIFormat(System.Windows.Media.PixelFormat format)
        {
            SlimDX.DXGI.Format convertedFormat = SlimDX.DXGI.Format.Unknown;
            if (format == System.Windows.Media.PixelFormats.Gray16)
            {
                convertedFormat = SlimDX.DXGI.Format.R16_UNorm;
            }
            else if (format == System.Windows.Media.PixelFormats.Gray8)
            {
                convertedFormat = SlimDX.DXGI.Format.R8_UNorm;
            }
            else if (format == System.Windows.Media.PixelFormats.Rgba64)
            {
                convertedFormat = SlimDX.DXGI.Format.R16G16B16A16_UNorm;
            }
            else if (format == System.Windows.Media.PixelFormats.Bgra32)
            {
                convertedFormat = SlimDX.DXGI.Format.B8G8R8A8_UNorm;
            }
            else if (format == System.Windows.Media.PixelFormats.Bgr32)
            {
                convertedFormat = SlimDX.DXGI.Format.B8G8R8X8_UNorm;
            }
            else if (format == System.Windows.Media.PixelFormats.Bgr24)
            {
                convertedFormat = SlimDX.DXGI.Format.B8G8R8X8_UNorm;
            }
            else if (format == System.Windows.Media.PixelFormats.Rgb48)
            {
                convertedFormat = SlimDX.DXGI.Format.R16G16B16A16_UNorm;
            }
            else
            {
                throw new NotSupportedException("Unsurpported image format!");
            }
            return convertedFormat;
        }
        #endregion Private Functions
    }
}
