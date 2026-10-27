using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SurfaceLayerHandler : BlockLayerHandler
{
    public BlockType surfaceBlockType;
    protected override bool TryHandling(ChunkData chunkData, int x, int y, int z, int surfaceHeightNoise, Vector2Int mapSeedOffset)
    {
        // y is local to the chunk; the surface height is a world height.
        if (chunkData.worldPosition.y + y == surfaceHeightNoise)
        {
            Vector3Int pos = new Vector3Int(x, y, z);
            Chunk.SetBlock(chunkData, pos, surfaceBlockType);
            return true;
        }
        return false;
    }
}