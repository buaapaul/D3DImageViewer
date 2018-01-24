//--------------------------------------------------------------------------------------
// File: BasicHLSL.fx
//
// The effect file for the BasicHLSL sample.  
// 
// Copyright (c) Microsoft Corporation. All rights reserved.
//--------------------------------------------------------------------------------------


//--------------------------------------------------------------------------------------
// Global variables
//--------------------------------------------------------------------------------------
//float4 g_MaterialAmbientColor;      // Material's ambient color
//float4 g_MaterialDiffuseColor;      // Material's diffuse color
//int g_nNumLights;

//float3 g_LightDir[3];               // Light's direction in world space
//float4 g_LightDiffuse[3];           // Light's diffuse color
//float4 g_LightAmbient;              // Light's ambient color

texture g_Texture;              // Color texture for mesh

float g_DisplayRangeH;			// normalized display range high value
float g_DisplayRangeL;			// normalized display range low value
//float    g_fTime;                   // App's time in seconds
//float4x4 g_mWorld;                  // World matrix for object
float4x4 g_mWorldViewProjection;    // World * View * Projection matrix



//--------------------------------------------------------------------------------------
// Texture samplers
//--------------------------------------------------------------------------------------
sampler MeshTextureSampler = 
sampler_state
{
    Texture = <g_Texture>;
    MipFilter = LINEAR;
    MinFilter = LINEAR;
    MagFilter = LINEAR;
};


//--------------------------------------------------------------------------------------
// Vertex shader output structure
//--------------------------------------------------------------------------------------
struct VS_OUTPUT
{
    float4 Position   : POSITION;   // vertex position 
//    float4 Diffuse    : COLOR0;     // vertex diffuse color (note that COLOR0 is clamped from 0..1)
    float2 TextureUV  : TEXCOORD0;  // vertex texture coords 
};


//--------------------------------------------------------------------------------------
// This shader computes standard transform and lighting
//--------------------------------------------------------------------------------------
VS_OUTPUT RenderSceneVS( float4 vPos : POSITION, 
                         float2 vTexCoord0 : TEXCOORD0
                       )
{
    VS_OUTPUT Output;
	//Output.Position = mul(float4(vPos, 1.0f), g_mWorldViewProjection);
	Output.Position = mul(vPos, g_mWorldViewProjection);
	Output.TextureUV = vTexCoord0;
    return Output;
}


//--------------------------------------------------------------------------------------
// Pixel shader output structure
//--------------------------------------------------------------------------------------
struct PS_OUTPUT
{
    float4 RGBColor : COLOR0;  // Pixel color    
};


//--------------------------------------------------------------------------------------
// This shader outputs the pixel's color by modulating the texture's
//       color with diffuse material color
//--------------------------------------------------------------------------------------
PS_OUTPUT RenderScenePS( VS_OUTPUT In
                         ) 
{ 
    PS_OUTPUT Output;

    Output.RGBColor = tex2D(MeshTextureSampler, In.TextureUV);
	Output.RGBColor = (Output.RGBColor - g_DisplayRangeL) / (g_DisplayRangeH - g_DisplayRangeL);

    return Output;
}


//--------------------------------------------------------------------------------------
// Renders scene to render target
//--------------------------------------------------------------------------------------
technique DefaultRenderingTechnique
{
    pass P0
    {          
        VertexShader = compile vs_2_0 RenderSceneVS(  );
        PixelShader  = compile ps_2_0 RenderScenePS(  ); // trivial pixel shader (could use FF instead if desired)
    }
}
