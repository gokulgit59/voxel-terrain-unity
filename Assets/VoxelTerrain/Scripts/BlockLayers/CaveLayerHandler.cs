using UnityEngine;

// Add this as the LAST entry in BiomeGenerator's additionalLayerHandlers list, after
// StoneLayerHandler/WaterLayerHandler, so it carves through blocks they already placed.
public class CaveLayerHandler : BlockLayerHandler
{
    [SerializeField]
    private NoiseSettings caveNoiseSettings;

    [Range(0, 1)]
    public float caveThreshold = 0.45f;

    // Don't carve within this many blocks of the surface, to avoid punching holes at ground level.
    public int surfaceBuffer = 3;

    // Don't carve below this world Y, to keep a solid floor under the world.
    public int worldFloor = 1;

    protected override bool TryHandling(ChunkData chunkData, int x, int y, int z, int surfaceHeightNoise, Vector2Int mapSeedOffset)
    {
        if (chunkData.worldPosition.y > surfaceHeightNoise)
            return false;

        caveNoiseSettings.worldOffset = mapSeedOffset;

        int startPosition = chunkData.worldPosition.y;
        int endPosition = Mathf.Min(chunkData.worldPosition.y + chunkData.chunkHeight - 1, surfaceHeightNoise - surfaceBuffer);

        bool carvedAny = false;
        for (int i = startPosition; i <= endPosition; i++)
        {
            if (i < worldFloor)
                continue;

            float caveNoise = MyNoise.OctavePerlin3D(chunkData.worldPosition.x + x, i, chunkData.worldPosition.z + z, caveNoiseSettings);
            if (caveNoise < caveThreshold)
            {
                Vector3Int pos = new Vector3Int(x, i - chunkData.worldPosition.y, z);
                Chunk.SetBlock(chunkData, pos, BlockType.Air);
                carvedAny = true;
            }
        }
        return carvedAny;
    }
}
