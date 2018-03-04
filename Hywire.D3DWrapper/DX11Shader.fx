//--------------------------------------------------------------------------------------
// Global variables
//--------------------------------------------------------------------------------------
float4x4 WorldViewProj;    // World * View * Projection matrix

Texture2D tex2D;
SamplerState samLinear
{
	Filter = MIN_MAG_MIP_POINT;
};

float g_DisplayRangeH;			// normalized display range high value
float g_DisplayRangeL;			// normalized display range low value

int g_RedChannelMap;
int g_GreenChannelMap;
int g_BlueChannelMap;
int g_AlphaChannelMap;

//--------------------------------------------------------------------------------------
// Vertex shader output structure
//--------------------------------------------------------------------------------------
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

PS_INPUT VS(VS_INPUT input)
{
	PS_INPUT output = (PS_INPUT)0;
	output.Pos = mul(input.Pos, WorldViewProj);
	//output.Pos = input.Pos;
	output.Tex = input.Tex;
	return output;
}

float4 PS(PS_INPUT input) : SV_Target
{
	float4 tmpOut;
	float4 output;

	tmpOut = tex2D.Sample(samLinear, input.Tex);
	output = tmpOut;

	tmpOut.rgb = (tmpOut.rgb - g_DisplayRangeL) / (g_DisplayRangeH - g_DisplayRangeL);

	if (g_RedChannelMap == 1) { output.r = tmpOut.g; }
	else if (g_RedChannelMap == 2) { output.r = tmpOut.b; }
	else if (g_RedChannelMap == 3) { output.r = tmpOut.a; }
	else { output.r = tmpOut.r; }

	if (g_GreenChannelMap == 0) { output.g = tmpOut.r; }
	else if (g_GreenChannelMap == 2) { output.g = tmpOut.b; }
	else if (g_GreenChannelMap == 3) { output.g = tmpOut.a; }
	else { output.g = tmpOut.g; }

	if (g_BlueChannelMap == 0) { output.b = tmpOut.r; }
	else if (g_BlueChannelMap == 1) { output.b = tmpOut.g; }
	else if (g_BlueChannelMap == 3) { output.b = tmpOut.a; }
	else { output.b = tmpOut.b; }

	if (g_AlphaChannelMap == 0) { output.a = tmpOut.r; }
	else if (g_AlphaChannelMap == 1) { output.a = tmpOut.g; }
	else if (g_AlphaChannelMap == 2) { output.a = tmpOut.b; }
	else { output.a = tmpOut.a; }

	return output;
}


//--------------------------------------------------------------------------------------
// Renders scene to render target
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
