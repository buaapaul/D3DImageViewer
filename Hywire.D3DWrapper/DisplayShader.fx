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

int g_RedChannelMap;
int g_GreenChannelMap;
int g_BlueChannelMap;
int g_AlphaChannelMap;

//--------------------------------------------------------------------------------------
// Texture samplers
//--------------------------------------------------------------------------------------
sampler MeshTextureSampler = 
sampler_state
{
    Texture = <g_Texture>;
    MipFilter = POINT;
    MinFilter = POINT;
    MagFilter = POINT;
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
	PS_OUTPUT tmpOut;
	PS_OUTPUT Output;

    tmpOut.RGBColor = tex2D(MeshTextureSampler, In.TextureUV);
	tmpOut.RGBColor.rgb = (tmpOut.RGBColor.rgb - g_DisplayRangeL) / (g_DisplayRangeH - g_DisplayRangeL);

	if (g_RedChannelMap == 1) { Output.RGBColor.r = tmpOut.RGBColor.g; }
	else if (g_RedChannelMap == 2) { Output.RGBColor.r = tmpOut.RGBColor.b; }
	else if (g_RedChannelMap == 3) { Output.RGBColor.r = tmpOut.RGBColor.a; }
	else { Output.RGBColor.r = tmpOut.RGBColor.r; }

	if (g_GreenChannelMap == 0) { Output.RGBColor.g = tmpOut.RGBColor.r; }
	else if (g_GreenChannelMap == 2) { Output.RGBColor.g = tmpOut.RGBColor.b; }
	else if (g_GreenChannelMap == 3) { Output.RGBColor.g = tmpOut.RGBColor.a; }
	else { Output.RGBColor.g = tmpOut.RGBColor.g; }

	if (g_BlueChannelMap == 0) { Output.RGBColor.b = tmpOut.RGBColor.r; }
	else if (g_BlueChannelMap == 1) { Output.RGBColor.b = tmpOut.RGBColor.g; }
	else if (g_BlueChannelMap == 3) { Output.RGBColor.b = tmpOut.RGBColor.a; }
	else { Output.RGBColor.b = tmpOut.RGBColor.b; }

	if (g_AlphaChannelMap == 0) { Output.RGBColor.a = tmpOut.RGBColor.r; }
	else if (g_AlphaChannelMap == 1) { Output.RGBColor.a = tmpOut.RGBColor.g; }
	else if (g_AlphaChannelMap == 2) { Output.RGBColor.a = tmpOut.RGBColor.b; }
	else { Output.RGBColor.a = tmpOut.RGBColor.a; }

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
