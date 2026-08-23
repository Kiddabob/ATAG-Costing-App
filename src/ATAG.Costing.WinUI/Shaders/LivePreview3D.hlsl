cbuffer SceneConstants : register(b0)
{
    float4x4 WorldViewProjection;
    float4 LightDirection;
    float4 CameraPosition;
};

struct VertexInput
{
    float3 Position : POSITION;
    float3 Normal : NORMAL;
    float4 Colour : COLOR;
};

struct PixelInput
{
    float4 Position : SV_POSITION;
    float3 Normal : NORMAL;
    float4 Colour : COLOR;
    float3 WorldPosition : TEXCOORD0;
};

PixelInput VSMain(VertexInput input)
{
    PixelInput output;
    output.Position = mul(float4(input.Position, 1.0f), WorldViewProjection);
    output.Normal = input.Normal;
    output.Colour = input.Colour;
    output.WorldPosition = input.Position;
    return output;
}

float4 PSMain(PixelInput input) : SV_TARGET
{
    float3 normal = normalize(input.Normal);
    float3 lightDirection = normalize(-LightDirection.xyz);
    float3 viewDirection = normalize(CameraPosition.xyz - input.WorldPosition);
    float diffuse = saturate(dot(normal, lightDirection));

    // Deliberately quantised, inexpensive lighting keeps adjacent strands
    // readable on integrated graphics and on the WARP fallback path.
    float diffuseBand = diffuse >= 0.72f
        ? 1.0f
        : (diffuse >= 0.34f ? 0.68f : 0.38f);
    float hemisphereFill = 0.08f * saturate((normal.y * 0.5f) + 0.5f);
    float3 halfDirection = normalize(lightDirection + viewDirection);
    float specular = pow(saturate(dot(normal, halfDirection)), 34.0f);
    float specularBand = specular >= 0.48f ? 0.16f : 0.0f;
    float facing = abs(dot(normal, viewDirection));
    float contour = smoothstep(0.10f, 0.34f, facing);
    float rim = smoothstep(0.62f, 0.92f, 1.0f - facing) * 0.12f;

    float lighting = 0.28f + (0.66f * diffuseBand) + hemisphereFill;
    float3 shaded = input.Colour.rgb * lighting;
    shaded *= lerp(0.48f, 1.0f, contour);
    shaded += specularBand.xxx;
    shaded += float3(0.34f, 0.68f, 0.88f) * rim;
    return float4(saturate(shaded), input.Colour.a);
}
