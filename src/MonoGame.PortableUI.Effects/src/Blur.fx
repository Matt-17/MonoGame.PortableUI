// PortableUI separable Gaussian blur (SpriteBatch-compatible: pixel shader only).
// Direction carries the per-tap UV step: (1/width, 0) * spread for the horizontal
// pass, (0, 1/height) * spread for the vertical pass.

sampler2D TextureSampler : register(s0);

float2 Direction;

static const float Weights[5] = { 0.227027, 0.1945946, 0.1216216, 0.054054, 0.016216 };

float4 MainPS(float4 position : SV_POSITION, float4 color : COLOR0, float2 uv : TEXCOORD0) : COLOR0
{
    float4 sum = tex2D(TextureSampler, uv) * Weights[0];
    [unroll]
    for (int i = 1; i < 5; i++)
    {
        float2 offset = Direction * i;
        sum += tex2D(TextureSampler, uv + offset) * Weights[i];
        sum += tex2D(TextureSampler, uv - offset) * Weights[i];
    }

    return sum * color;
}

technique Blur
{
    pass P0
    {
        PixelShader = compile ps_3_0 MainPS();
    }
}
