using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class WaterLayerHandler : BlockLayerHandler
{
    public int waterLevel = 1;
    protected override bool TryHandling(ChunkData chunkData, int x, int y, int z, int surfaceHeightNoise, Vector2Int mapSeedOffset)
    {
        // y is local to the chunk; the surface height and water level are world heights.
        int worldY = chunkData.worldPosition.y + y;
        if(worldY > surfaceHeightNoise && worldY <= waterLevel)
        {
            Vector3Int pos = new Vector3Int(x, y, z);
            Chunk.SetBlock(chunkData, pos, BlockType.Water);
            if(worldY == surfaceHeightNoise + 1)
            {
                pos.y = y - 1;
                Chunk.SetBlock(chunkData, pos, BlockType.Sand);
            }
            return true;
        }
        return false;
    }
}