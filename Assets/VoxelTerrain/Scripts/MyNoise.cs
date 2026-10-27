using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public static class MyNoise
{
    public static float RemapValue(float value, float initialMin, float initialMax, float outputMin, float outputMax)
    {
        return outputMin + (value - initialMin) * (outputMax - outputMin) / (initialMax - initialMin);
    }

    public static float RemapValue01(float value, float outputMin, float outputMax)
    {
        return outputMin + (value - 0) * (outputMax - outputMin) / (1 - 0);
    }

    public static int RemapValue01ToInt(float value, float outputMin, float outputMax)
    {
        return (int)RemapValue01(value, outputMin, outputMax);
    }

    public static float Redistribution(float noise, NoiseSettings settings)
    {
        return Mathf.Pow(noise * settings.redistributionModifier, settings.exponent);
    }

    public static float OctavePerlin(float x, float z, NoiseSettings settings)
    {
        x *= settings.noiseZoom;
        z *= settings.noiseZoom;
        x += settings.noiseZoom;
        z += settings.noiseZoom;

        float total = 0;
        float frequency = 1;
        float amplitude = 1;
        float amplitudeSum = 0;  // Used for normalizing result to 0.0 - 1.0 range
        for (int i = 0; i < settings.octaves; i++)
        {
            total += Mathf.PerlinNoise((settings.offest.x + settings.worldOffset.x + x) * frequency, (settings.offest.y + settings.worldOffset.y + z) * frequency) * amplitude;

            amplitudeSum += amplitude;

            amplitude *= settings.persistance;
            frequency *= 2;
        }

        return total / amplitudeSum;
    }

    // Unity has no native 3D Perlin noise, so this approximates it by sampling the
    // three axis-aligned planes through (x, y, z) and averaging them.
    public static float OctavePerlin3D(float x, float y, float z, NoiseSettings settings)
    {
        x *= settings.noiseZoom;
        y *= settings.noiseZoom;
        z *= settings.noiseZoom;

        float total = 0;
        float frequency = 1;
        float amplitude = 1;
        float amplitudeSum = 0;

        for (int i = 0; i < settings.octaves; i++)
        {
            float ox = (settings.offest.x + settings.worldOffset.x + x) * frequency;
            float oy = (settings.offest.y + y) * frequency;
            float oz = (settings.offest.x + settings.worldOffset.y + z) * frequency;

            float xy = Mathf.PerlinNoise(ox, oy);
            float yz = Mathf.PerlinNoise(oy, oz);
            float xz = Mathf.PerlinNoise(ox, oz);
            float yx = Mathf.PerlinNoise(oy, ox);
            float zy = Mathf.PerlinNoise(oz, oy);
            float zx = Mathf.PerlinNoise(oz, ox);

            total += ((xy + yz + xz + yx + zy + zx) / 6f) * amplitude;
            amplitudeSum += amplitude;

            amplitude *= settings.persistance;
            frequency *= 2;
        }

        return total / amplitudeSum;
    }
}