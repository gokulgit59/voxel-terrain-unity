using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class TerrainGenerator : MonoBehaviour
{
    // Fallback used when biomeSelector is unset (or has no entries) - keeps single-biome setups working unchanged.
    public BiomeGenerator biomeGenerator;

    // Optional: when assigned, picks between multiple BiomeGenerators per column instead of
    // always using biomeGenerator.
    public BiomeSelector biomeSelector;

    public ChunkData GenerateChunkData(ChunkData data, Vector2Int mapSeedOffset)
    {
        for (int x = 0; x < data.chunkSize; x++)
        {
            for (int z = 0; z < data.chunkSize; z++)
            {
                BiomeGenerator biome = null;
                if (biomeSelector != null)
                {
                    biome = biomeSelector.SelectBiome(data.worldPosition.x + x, data.worldPosition.z + z, mapSeedOffset);
                }
                if (biome == null)
                {
                    biome = biomeGenerator;
                }

                data = biome.ProcessChunkColumn(data, x, z, mapSeedOffset);
            }
        }
        return data;
    }
}