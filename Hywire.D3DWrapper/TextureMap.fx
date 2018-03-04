// THIS CODE AND INFORMATION IS PROVIDED "AS IS" WITHOUT WARRANTY OF
// ANY KIND, EITHER EXPRESSED OR IMPLIED, INCLUDING BUT NOT LIMITED TO
// THE IMPLIED WARRANTIES OF MERCHANTABILITY AND/OR FITNESS FOR A
// PARTICULAR PURPOSE.
//
// Copyright (c) Microsoft Corporation. All rights reserved

//--------------------------------------------------------------------------------------
// Constant Buffer Variables
//--------------------------------------------------------------------------------------
float4x4 WorldMatrix;

Texture2D txDiffuse;
SamplerState samLinear
{
    Filter = MIN_MAG_MIP_LINEAR;
    //Filter = MIN_MAG_MIP_POINT;

	//AddressU = Wrap;
 //   AddressV = Wrap;
};

struct VS_INPUT
{
    float4 Pos : POSITION;
    float2 Tex : TEXCOORD;
};

struct PS_INPUT
{
    float4 Pos : SV_POSITION;
    float2 Tex : TEXCOORD0;
};


//--------------------------------------------------------------------------------------
// Vertex Shader
//--------------------------------------------------------------------------------------
PS_INPUT VS( VS_INPUT input )
{
    PS_INPUT output = (PS_INPUT)0;
    output.Tex = input.Tex;
	output.Pos = input.Pos;
    //output.Pos = mul(input.Pos, WorldMatrix);
    return output;
}


//--------------------------------------------------------------------------------------
// Pixel Shader
//--------------------------------------------------------------------------------------
float4 PS( PS_INPUT input) : SV_Target
{
	float4 color = txDiffuse.Sample( samLinear, input.Tex );
	//if((color.r==0)&&(color.g==0)&&color.b==0)
	//{
	//	color.rgb=1;
	//}
	//else
	//{
	//	color.rgb=0;
	//}
	color.rgb = (color.rgb-0.0/65535)/(300.0/65535.0-0.0/65535.0);
    return color;
}


//--------------------------------------------------------------------------------------
technique10 Render
{
    pass P0
    {		
        SetVertexShader( CompileShader( vs_4_0, VS() ) );
        SetGeometryShader( NULL );
        SetPixelShader( CompileShader( ps_4_0, PS() ) );
    }
}

