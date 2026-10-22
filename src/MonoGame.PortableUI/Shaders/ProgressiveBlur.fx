// PortableUI progressive blur (SpriteBatch-compatible: pixel shaders only). A band of content is blurred with a
// radius that falls from the edge row to zero; uv covers the band, so the strength of a row is the smoothstep of
// 1 - |uv.y - EdgeV| * Slope (eased in, so the blur sets in without a visible start).
//
// Blur: one separable Gaussian pass. The taps spread with the row's radius (12 per side, sigma = radius / 2),
// so their weights stay constant. Direction is one texel along the pass, MaxRadius the edge radius in texels.
// Compose: the sharp content and the blurred band mixed by the row's radius (fully blurred from FadeRadius
// pixels on), so the band runs out into the untouched content without a seam; then Tint (premultiplied) laid over
// it, scaled by the row's strength, and half a step of noise so long soft gradients do not band.

sampler2D TextureSampler : register(s0);

texture SharpTexture;
sampler2D SharpSampler = sampler_state
{
    Texture = <SharpTexture>;
    MinFilter = Point;
    MagFilter = Point;
    AddressU = Clamp;
    AddressV = Clamp;
};

float2 Direction;
float EdgeV;
float Slope;
float MaxRadius;
float4 SharpRect;
float FadeRadius;
float4 Tint;

static const float Weights[13] = { 1.000000, 0.986207, 0.945959, 0.882497, 0.800737, 0.706648, 0.606531, 0.506336, 0.411112, 0.324652, 0.249352, 0.186270, 0.135335 };

float Strength(float v)
{
    return smoothstep(0.0, 1.0, 1.0 - abs(v - EdgeV) * Slope);
}

float4 BlurPS(float4 position : SV_POSITION, float4 color : COLOR0, float2 uv : TEXCOORD0) : COLOR0
{
    float2 step = Direction * (Strength(uv.y) * MaxRadius / 12.0);
    float4 sum = tex2D(TextureSampler, uv);
    float total = 1.0;
    [unroll]
    for (int i = 1; i < 13; i++)
    {
        sum += (tex2D(TextureSampler, uv + step * i) + tex2D(TextureSampler, uv - step * i)) * Weights[i];
        total += 2.0 * Weights[i];
    }
    return sum / total;
}

float4 ComposePS(float4 position : SV_POSITION, float4 color : COLOR0, float2 uv : TEXCOORD0) : COLOR0
{
    float4 sharp = tex2D(SharpSampler, SharpRect.xy + uv * SharpRect.zw);
    float4 blurred = tex2D(TextureSampler, uv);
    float strength = Strength(uv.y);
    float weight = saturate(strength * MaxRadius / FadeRadius);
    float4 mixed = lerp(sharp, blurred, weight);
    float4 result = Tint * strength + mixed * (1.0 - Tint.a * strength);
    float noise = frac(sin(dot(floor(uv * 4096.0), float2(12.9898, 78.233))) * 43758.5453) - 0.5;
    result.rgb += noise / 255.0 * result.a;
    return result * color;
}

technique Blur
{
    pass P0
    {
        PixelShader = compile ps_3_0 BlurPS();
    }
}

technique Compose
{
    pass P0
    {
        PixelShader = compile ps_3_0 ComposePS();
    }
}
