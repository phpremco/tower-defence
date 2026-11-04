// Shared helpers of the space shaders: seamless 3D value noise on the unit sphere (so nothing has a texture seam),
// fractal sums of it and the direction-to-map mapping of PlanetCoordinates.ToUv.
#ifndef GAME_PLANET_COMMON_INCLUDED
#define GAME_PLANET_COMMON_INCLUDED

float PlanetHash(float3 p)
{
    p = frac(p * 0.3183099 + 0.1);
    p *= 17.0;
    return frac(p.x * p.y * p.z * (p.x + p.y + p.z));
}

float PlanetNoise(float3 x)
{
    float3 i = floor(x);
    float3 f = frac(x);
    f = f * f * (3.0 - 2.0 * f);
    return lerp(
        lerp(lerp(PlanetHash(i + float3(0, 0, 0)), PlanetHash(i + float3(1, 0, 0)), f.x),
             lerp(PlanetHash(i + float3(0, 1, 0)), PlanetHash(i + float3(1, 1, 0)), f.x), f.y),
        lerp(lerp(PlanetHash(i + float3(0, 0, 1)), PlanetHash(i + float3(1, 0, 1)), f.x),
             lerp(PlanetHash(i + float3(0, 1, 1)), PlanetHash(i + float3(1, 1, 1)), f.x), f.y), f.z);
}

// Fractal noise 0..1; the octave count comes from the quality profile, so the loop bound is a material value.
float PlanetFbm(float3 p, int octaves)
{
    float sum = 0.0;
    float amplitude = 0.5;
    float norm = 0.0;
    [loop]
    for (int i = 0; i < octaves; i++)
    {
        sum += PlanetNoise(p) * amplitude;
        norm += amplitude;
        p = p * 2.03 + float3(1.7, 9.2, 4.1);
        amplitude *= 0.5;
    }

    return sum / max(norm, 0.0001);
}

// Sharp crests where the noise crosses its middle: mountain chains and crystal ridges.
float PlanetRidged(float3 p, int octaves)
{
    float sum = 0.0;
    float amplitude = 0.5;
    float norm = 0.0;
    [loop]
    for (int i = 0; i < octaves; i++)
    {
        float n = 1.0 - abs(PlanetNoise(p) * 2.0 - 1.0);
        sum += n * n * amplitude;
        norm += amplitude;
        p = p * 2.1 + float3(5.3, 1.1, 7.7);
        amplitude *= 0.5;
    }

    return sum / max(norm, 0.0001);
}

// The equirectangular map position of a direction, as PlanetCoordinates.ToUv lays it out.
float2 PlanetDirectionToUv(float3 d)
{
    float longitude = atan2(-d.x, d.z);
    float latitude = asin(clamp(d.y, -1.0, 1.0));
    return float2(longitude * 0.15915494 + 0.5, latitude * 0.31830989 + 0.5);
}

#endif
