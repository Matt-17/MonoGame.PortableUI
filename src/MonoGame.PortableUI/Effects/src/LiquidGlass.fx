// PortableUI liquid glass (SpriteBatch-compatible: pixel shader only).
// The sprite is the sharp backdrop, drawn over the element's rect with the matching source
// rect, so uv runs from UvMin to UvMax across the element. A rounded-rect distance field shapes
// the glass: near the rim the backdrop is refracted inward like a lens (with a little chromatic
// fringe), a light rim catches the light from the top left, the body stays almost clear.

sampler2D SceneSampler : register(s0);   // sharp backdrop
sampler2D BlurSampler : register(s1);    // blurred backdrop (same screen mapping)

float2 RectSize;      // element size in pixels
float Radius;         // corner radius in pixels
float Bezel;          // width of the refracting rim in pixels
float Refraction;     // max displacement at the rim in pixels
float Chroma;         // chromatic fringe (fraction of the displacement)
float Frost;          // 0 = clear, 1 = fully blurred body
float4 Tint;          // premultiplied tint laid over the glass
float Highlight;      // strength of the specular rim
float2 UvMin;
float2 UvMax;
float2 TexelSize;     // 1 / backdrop size

float RoundedBox(float2 p, float2 halfSize, float r)
{
    float2 q = abs(p) - halfSize + r;
    return length(max(q, 0.0)) + min(max(q.x, q.y), 0.0) - r;
}

float4 MainPS(float4 position : SV_POSITION, float4 color : COLOR0, float2 uv : TEXCOORD0) : COLOR0
{
    float2 span = max(UvMax - UvMin, float2(1e-6, 1e-6));
    float2 local = (uv - UvMin) / span * RectSize;
    float2 p = local - RectSize * 0.5;
    float2 halfSize = RectSize * 0.5;
    float r = min(Radius, min(halfSize.x, halfSize.y));

    float d = RoundedBox(p, halfSize, r);
    // Outward normal from the distance field's gradient.
    float2 e = float2(0.5, 0.0);
    float2 n = float2(RoundedBox(p + e.xy, halfSize, r) - RoundedBox(p - e.xy, halfSize, r),
                      RoundedBox(p + e.yx, halfSize, r) - RoundedBox(p - e.yx, halfSize, r));
    n = n / max(length(n), 1e-5);

    // 0 at the rim, 1 once past the bezel: a squared falloff gives the lens-like edge.
    float inside = saturate(-d / max(Bezel, 1.0));
    float edge = 1.0 - inside;
    float lens = edge * edge;
    float2 shift = -n * Refraction * lens * TexelSize;

    float red = tex2D(SceneSampler, uv + shift * (1.0 + Chroma)).r;
    float green = tex2D(SceneSampler, uv + shift).g;
    float blue = tex2D(SceneSampler, uv + shift * (1.0 - Chroma)).b;
    float3 sharp = float3(red, green, blue);
    float3 soft = tex2D(BlurSampler, uv + shift).rgb;
    float3 glass = lerp(sharp, soft, Frost);

    // Tint, then light: a bright rim facing the top-left light, a faint dark rim at the bottom.
    glass = glass * (1.0 - Tint.a) + Tint.rgb;
    float rim = pow(edge, 6.0);
    float facing = saturate(dot(-n, normalize(float2(0.6, 0.8))));
    float shade = saturate(dot(n, normalize(float2(0.3, 1.0))));
    glass += Highlight * rim * (0.35 + 0.65 * facing);
    glass *= 1.0 - 0.18 * rim * shade;
    // Soft inner sheen across the top half.
    glass += Highlight * 0.06 * saturate(1.0 - local.y / max(RectSize.y * 0.5, 1.0));

    float alpha = saturate(0.5 - d);
    return float4(glass * alpha, alpha) * color;
}

technique LiquidGlass
{
    pass P0
    {
        PixelShader = compile ps_3_0 MainPS();
    }
}
