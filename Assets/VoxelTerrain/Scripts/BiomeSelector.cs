using System;
using System.Collections.Generic;
using UnityEngine;

[Serializable]
public class BiomeEntry
{
    public BiomeGenerator biome;

    // The selection noise value (0..1) must be >= this for the entry to be a candidate.
    // The entry with the highest minValue that the noise still clears wins - so order entries
    // however you like, just keep minValue ascending with "how rare/extreme" the biome is.
    [Range(0, 1)]
    public float minValue;
}

// Picks which BiomeGenerator handles each column using a single low-frequency noise map
// (think of it as a simple 1D temperature/elevation axis). Each BiomeGenerator keeps its own
// height noise, water level and block-layer chain, exactly like the original single-biome setup.
public class BiomeSelector : MonoBehaviour
{
    public NoiseSettings biomeSelectionNoise;
    public List<BiomeEntry> biomes = new List<BiomeEntry>();

    public BiomeGenerator SelectBiome(int worldX, int worldZ, Vector2Int mapSeedOffset)
    {
        if (biomes == null || biomes.Count == 0)
            return null;

        biomeSelectionNoise.worldOffset = mapSeedOffset;
        float value = MyNoise.OctavePerlin(worldX, worldZ, biomeSelectionNoise);

        BiomeGenerator selected = biomes[0].biome;
        float bestMin = float.NegativeInfinity;
        foreach (BiomeEntry entry in biomes)
        {
            if (entry.biome != null && value >= entry.minValue && entry.minValue >= bestMin)
            {
                selected = entry.biome;
                bestMin = entry.minValue;
            }
        }
        return selected;
    }
}
