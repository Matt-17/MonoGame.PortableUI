// PortableUI primitives shader (SpriteBatch-compatible tint pass-through).
// Placeholder body for the future SDF rounded-rect/border path; kept minimal but valid
// so RenderCapabilities.ShadersAvailable reflects a working shader pipeline.

sampler2D TextureSampler : register(s0);

float4 MainPS(float4 position : SV_POSITION, float4 color : COLOR0, float2 uv : TEXCOORD0) : COLOR0
{
    return tex2D(TextureSampler, uv) * color;
}

technique SpriteBatch
{
    pass P0
    {
        PixelShader = compile ps_3_0 MainPS();
    }
}
