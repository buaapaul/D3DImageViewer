using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using SlimDX;
using SlimDX.Direct3D11;
using SlimDX.Windows;
using SlimDX.D3DCompiler;

namespace Ex02_Texture
{
    struct CustomVertex
    {
        public Vector3 Pos;
        public Vector2 Tex;
    }
    class Program
    {
        static RenderForm form;
        static Device _Device;
        static SlimDX.DXGI.SwapChain _SwapChain;
        static RenderTargetView _ViewRenderTarget;
        static VertexShader _VertexShader;
        static InputLayout _LayOut;
        static PixelShader _PixelShader;
        private static SlimDX.Direct3D11.Buffer _VertexBuffer;
        private static SlimDX.Direct3D11.Buffer _IndexBuffer;
        private static ShaderResourceView _TextureRV;
        private static SamplerState _SamplerLinear;

        private static ShaderResourceView _TextureRV2;

        static void Main(string[] args)
        {
            form = new RenderForm("Simple Texture Example");

            if (InitDevice().IsFailure)
            {
                CleanUpDevice();
                return;
            }

            MessagePump.Run(form, () =>
            {
                Render();
            });

            CleanUpDevice();
        }

        static Result InitDevice()
        {
            //int width = form.ClientSize.Width;
            //int height = form.ClientSize.Height;
            int width = 2500;
            int height = 1000;
            SlimDX.DXGI.SwapChainDescription swapChainDesc = new SlimDX.DXGI.SwapChainDescription()
            {
                BufferCount = 1,
                ModeDescription = new SlimDX.DXGI.ModeDescription()
                {
                    Width = width,
                    Height = height,
                    Format = SlimDX.DXGI.Format.B8G8R8A8_UNorm,
                    RefreshRate = new Rational(60, 1),
                },
                Usage = SlimDX.DXGI.Usage.RenderTargetOutput,
                OutputHandle = form.Handle,
                SampleDescription = new SlimDX.DXGI.SampleDescription { Count = 1, Quality = 0 },
                IsWindowed = true,
            };
            Device.CreateWithSwapChain(DriverType.Hardware, DeviceCreationFlags.None, new FeatureLevel[] { FeatureLevel.Level_11_0 }, swapChainDesc, out _Device, out _SwapChain);
            if (Result.Last.IsFailure)
            {
                return Result.Last;
            }

            // Create a render target view
            Texture2D backBuffer = Resource.FromSwapChain<Texture2D>(_SwapChain, 0);
            _ViewRenderTarget = new RenderTargetView(_Device, backBuffer);
            backBuffer.Dispose();
            if (Result.Last.IsFailure)
            {
                return Result.Last;
            }
            _Device.ImmediateContext.OutputMerger.SetTargets(_ViewRenderTarget);

            // Setup the viewport
            Viewport vp = new Viewport
            {
                Width = width,
                Height = height,
                MinZ = 0.0f,
                MaxZ = 1.0f,
                X = 0,
                Y = 0,
            };
            _Device.ImmediateContext.Rasterizer.SetViewports(vp);

            // Compile the vertex shader
            ShaderBytecode vertexShaderCode = ShaderBytecode.CompileFromFile("haha.fx", "VS", "vs_4_0", ShaderFlags.EnableStrictness | ShaderFlags.Debug, EffectFlags.None);
            if (Result.Last.IsFailure)
            {
                return Result.Last;
            }

            // Create the vertex shader
            _VertexShader = new VertexShader(_Device, vertexShaderCode);
            if (Result.Last.IsFailure)
            {
                return Result.Last;
            }

            // Define the input layout
            InputElement[] inputElements = new InputElement[2]
            {
                new InputElement("POSITION",0, SlimDX.DXGI.Format.R32G32B32_Float, 0,0, InputClassification.PerVertexData, 0),
                new InputElement("TEXCOORD",0, SlimDX.DXGI.Format.R32G32_Float, 12,0, InputClassification.PerVertexData, 0),
            };
            _LayOut = new InputLayout(_Device, vertexShaderCode, inputElements);
            vertexShaderCode.Dispose();
            if (Result.Last.IsFailure)
            {
                return Result.Last;
            }

            // Set the input layout
            _Device.ImmediateContext.InputAssembler.InputLayout = _LayOut;

            // Compile the pixel shader
            ShaderBytecode pixelShaderCode = ShaderBytecode.CompileFromFile("haha.fx", "PS", "ps_4_0", ShaderFlags.EnableStrictness | ShaderFlags.Debug, EffectFlags.None);
            if (Result.Last.IsFailure)
            {
                return Result.Last;
            }

            // Create the pixel shader
            _PixelShader = new PixelShader(_Device, pixelShaderCode);
            pixelShaderCode.Dispose();
            if (Result.Last.IsFailure)
            {
                return Result.Last;
            }

            // Create vertex buffer
            CustomVertex[] vertices = new CustomVertex[]
            {
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
            if (Result.Last.IsFailure)
            {
                return Result.Last;
            }

            // Set vertex buffer
            _Device.ImmediateContext.InputAssembler.SetVertexBuffers(0, new VertexBufferBinding(_VertexBuffer, stride, 0));

            // Create index buffer
            int[] indices = new int[]
            {
                0,1,2,
                1,3,2,

                4,5,6,
                5,7,6,

                8,9,10,
                9,11,10,

                12,13,14,
                13,15,14,
            };
            bufferDesc.SizeInBytes = sizeof(int) * indices.Length;
            bufferDesc.BindFlags = BindFlags.IndexBuffer;
            _IndexBuffer = new SlimDX.Direct3D11.Buffer(_Device, new DataStream(indices, false, false), bufferDesc);
            if (Result.Last.IsFailure)
            {
                return Result.Last;
            }

            // Set index buffer
            _Device.ImmediateContext.InputAssembler.SetIndexBuffer(_IndexBuffer, SlimDX.DXGI.Format.R32_UInt, 0);

            // Set primitive topology
            _Device.ImmediateContext.InputAssembler.PrimitiveTopology = PrimitiveTopology.TriangleList;

            // Load the Texture
            ImageLoadInformation imageLoadInfo = new ImageLoadInformation
            {
                Format = SlimDX.DXGI.Format.R16_UNorm,
                BindFlags = BindFlags.ShaderResource,
            };
            _TextureRV = ShaderResourceView.FromFile(_Device, "haha.dds", imageLoadInfo);
            _TextureRV2 = ShaderResourceView.FromFile(_Device, "haha.tif", imageLoadInfo);
            if (Result.Last.IsFailure)
            {
                return Result.Last;
            }

            // Create the sample state
            SamplerDescription sampDesc = new SamplerDescription
            {
                Filter = Filter.MinMagMipPoint,
                AddressU = TextureAddressMode.Wrap,
                AddressV = TextureAddressMode.Wrap,
                AddressW = TextureAddressMode.Wrap,
                ComparisonFunction = Comparison.Never,
                MinimumLod = 0,
                MaximumLod = float.MaxValue,
            };
            _SamplerLinear = SamplerState.FromDescription(_Device, sampDesc);
            if (Result.Last.IsFailure)
            {
                return Result.Last;
            }

            return ResultCode.Success;
        }

        static void CleanUpDevice()
        {
            if (!_Device.ImmediateContext.Disposed) _Device.ImmediateContext.ClearState();

            if (!_VertexBuffer.Disposed) _VertexBuffer.Dispose();
            if (!_LayOut.Disposed) _LayOut.Dispose();
            if (!_VertexShader.Disposed) _VertexShader.Dispose();
            if (!_PixelShader.Disposed) _PixelShader.Dispose();
            if (!_ViewRenderTarget.Disposed) _ViewRenderTarget.Dispose();
            if (!_SwapChain.Disposed) _SwapChain.Dispose();
            if (!_Device.ImmediateContext.Disposed) _Device.ImmediateContext.Dispose();
            if (!_Device.Disposed) _Device.Dispose();
        }

        static void Render()
        {
            // Clear the back buffer
            Color4 ClearColor = new Color4(System.Drawing.Color.Azure);
            _Device.ImmediateContext.ClearRenderTargetView(_ViewRenderTarget, ClearColor);

            // Render a triangle
            _Device.ImmediateContext.VertexShader.Set(_VertexShader);
            _Device.ImmediateContext.PixelShader.Set(_PixelShader);
            _Device.ImmediateContext.PixelShader.SetShaderResource(_TextureRV, 0);
            _Device.ImmediateContext.PixelShader.SetSampler(_SamplerLinear, 0);
            _Device.ImmediateContext.DrawIndexed(6, 0, 0);

            _Device.ImmediateContext.VertexShader.Set(_VertexShader);
            _Device.ImmediateContext.PixelShader.Set(_PixelShader);
            _Device.ImmediateContext.PixelShader.SetShaderResource(_TextureRV2, 0);
            _Device.ImmediateContext.PixelShader.SetSampler(_SamplerLinear, 0);
            _Device.ImmediateContext.DrawIndexed(18, 6, 0);

            // Present the information rendered to the back buffer to the front buffer (the screen)
            _SwapChain.Present(0, SlimDX.DXGI.PresentFlags.None);
        }
    }
}
