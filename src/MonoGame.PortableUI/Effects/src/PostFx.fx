// PortableUI single-pass post-process overlays (SpriteBatch-compatible: pixel shader only).
// Applies scanlines, dot-matrix grid, vignette and film grain while drawing the UI
// texture, replacing the shader-free overlay-texture chain in one pass.

sampler2D TextureSampler : register(s0);

float2 TexelSize;         // 1 / texture size in UV
float2 SourceSize;        // composed region size in pixels
float ScanlineStrength;   // 0..1
float ScanlineSpacing;    // pixels between dark lines (>= 2)
float DotMatrixStrength;  // 0..1
float DotMatrixCellSize;  // cell size in pixels (>= 2)
float VignetteStrength;   // 0..1
float GrainStrength;      // 0..1
float GrainSeed;          // random per frame

float Random(float2 seed)
{
    return frac(sin(dot(seed, float2(12.9898, 78.233))) * 43758.5453);
}

float4 MainPS(float4 position : SV_POSITION, float4 color : COLOR0, float2 uv : TEXCOORD0) : COLOR0
{
    float4 sample = tex2D(TextureSampler, uv) * color;
    float2 pixel = uv * SourceSize;

    if (ScanlineStrength > 0)
    {
        float scanPos = fmod(pixel.y, ScanlineSpacing);
        float dark = step(ScanlineSpacing - 1.0, scanPos);
        sample.rgb *= 1.0 - dark * ScanlineStrength;
    }

    if (DotMatrixStrength > 0)
    {
        float2 cell = fmod(pixel, DotMatrixCellSize);
        float grid = max(step(DotMatrixCellSize - 1.0, cell.x), step(DotMatrixCellSize - 1.0, cell.y));
        sample.rgb *= 1.0 - grid * DotMatrixStrength;
    }

    if (VignetteStrength > 0)
    {
        float2 centered = uv * 2.0 - 1.0;
        float distance = length(centered) / 1.41421356;
        float falloff = saturate((distance - 0.45) / 0.55);
        sample.rgb *= 1.0 - falloff * falloff * VignetteStrength;
    }

    if (GrainStrength > 0)
    {
        float noise = Random(uv * SourceSize + GrainSeed) - 0.5;
        sample.rgb += noise * GrainStrength * sample.a;
    }

    return sample;
}

technique PostFx
{
    pass P0
    {
        PixelShader = compile ps_3_0 MainPS();
    }
}
