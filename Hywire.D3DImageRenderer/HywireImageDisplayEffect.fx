
texture g_Texture;
float4x4 g_WorldViewProj;
float g_DisplayRangeH;
float g_DisplayRangeL;

sampler TexSampler = 
sampler_state
{
	Texture = <g_Texture>;
	MinFilter = LINEAR;
	MagFilter = LINEAR;
	MipFilter = LINEAR;
};

struct Shader_DataType
{
	float4 Position : POSITION;
	float2 TextureUV : TEXCOORD0;
};

Shader_DataType VS(Shader_DataType vin)
{
	Shader_DataType vout;
	vout.Position = mul(vin.Position, g_WorldViewProj);
	vout.TextureUV = vin.TextureUV;
	return vout;
}

float4 PS(Shader_DataType input) : COLOR0
{
	float4 color = tex2D(TexSampler, input.TextureUV);
	color.rgb = (color.rgb - g_DisplayRangeL) / (g_DisplayRangeH - g_DisplayRangeL);
	return color;
}

technique RenderScene
{
	pass p0
	{
		VertexShader = compile vs_2_0 VS();
		PixelShader = compile ps_2_0 PS();
	}
}