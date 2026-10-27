using UnityEngine;

// Add this as the LAST entry in BiomeGenerator's additionalLayerHandlers list.
// Trees are clipped to the placing chunk's bounds (no cross-chunk overhang) to avoid the
// local/world coordinate mixup in Chunk.SetBlock's out-of-range fallback.
public class TreeDecorationHandler : BlockLayerHandler
{
    [SerializeField]
    private NoiseSettings treeNoiseSettings;

    [Range(0, 1)]
    public float treeDensityThreshold = 0.55f;

    public int minTrunkHeight = 4;
    public int maxTrunkHeight = 6;
    public int canopyRadius = 2;

    // World is divided into treeSpacing x treeSpacing cells; at most one tree can anchor per
    // cell, so trees can never end up on adjacent columns. Must be > 2*canopyRadius+1 or
    // neighboring cells' canopies can still touch/overlap.
    public int treeSpacing = 8;

    protected override bool TryHandling(ChunkData chunkData, int x, int y, int z, int surfaceHeightNoise, Vector2Int mapSeedOffset)
    {
        if (chunkData.worldPosition.y > surfaceHeightNoise || chunkData.worldPosition.y + chunkData.chunkHeight <= surfaceHeightNoise)
            return false;

        // Skip trees whose canopy would cross into a neighboring chunk: that chunk may not be
        // generated yet during initial world creation, which would silently clip the canopy
        // and produce a lopsided tree. Keeping the whole tree inside this chunk guarantees a
        // full, symmetric canopy every time.
        if (x - canopyRadius < 0 || x + canopyRadius >= chunkData.chunkSize ||
            z - canopyRadius < 0 || z + canopyRadius >= chunkData.chunkSize)
            return false;

        int worldX = chunkData.worldPosition.x + x;
        int worldZ = chunkData.worldPosition.z + z;

        if (!TryGetTreeAnchor(worldX, worldZ, mapSeedOffset, out int trunkHeight))
            return false;

        BlockType surfaceBlock = Chunk.GetBlockFromChunkCoordinates(chunkData, x, surfaceHeightNoise, z);
        if (surfaceBlock != BlockType.Grass_Dirt)
            return false;

        for (int i = 1; i <= trunkHeight; i++)
        {
            TrySetLocalBlock(chunkData, x, surfaceHeightNoise + i, z, BlockType.TreeTrunk);
        }

        int canopyBaseY = surfaceHeightNoise + trunkHeight - canopyRadius;
        for (int cx = -canopyRadius; cx <= canopyRadius; cx++)
        {
            for (int cz = -canopyRadius; cz <= canopyRadius; cz++)
            {
                for (int cy = 0; cy <= canopyRadius; cy++)
                {
                    if (cx == 0 && cz == 0 && cy < canopyRadius)
                        continue;

                    float dist = new Vector3(cx, cy, cz).magnitude;
                    if (dist > canopyRadius + 0.5f)
                        continue;

                    int leafX = x + cx;
                    int leafY = canopyBaseY + cy;
                    int leafZ = z + cz;

                    if (!InChunkBounds(chunkData, leafX, leafY, leafZ))
                        continue;

                    BlockType existing = Chunk.GetBlockFromChunkCoordinates(chunkData, leafX, leafY, leafZ);
                    if (existing == BlockType.Air)
                    {
                        Chunk.SetBlock(chunkData, new Vector3Int(leafX, leafY, leafZ), BlockType.TreeLeafsSolid);
                    }
                }
            }
        }

        return true;
    }

    // A tree can only spawn at the single jittered anchor point of its treeSpacing x treeSpacing
    // cell, so no two trees can ever land on neighboring columns. The jitter is kept away from
    // the cell edges by canopyRadius so two trees from adjacent cells can't touch canopies.
    private bool TryGetTreeAnchor(int worldX, int worldZ, Vector2Int mapSeedOffset, out int trunkHeight)
    {
        trunkHeight = 0;

        int spacing = Mathf.Max(2 * canopyRadius + 2, treeSpacing);
        int margin = Mathf.Clamp(canopyRadius, 0, (spacing - 1) / 2);
        int jitterRange = spacing - 2 * margin;

        int cellX = FloorDiv(worldX, spacing);
        int cellZ = FloorDiv(worldZ, spacing);

        System.Random cellRng = new System.Random(cellX * 92821 ^ cellZ * 68917 ^ mapSeedOffset.x ^ mapSeedOffset.y);
        int anchorX = cellX * spacing + margin + cellRng.Next(0, jitterRange);
        int anchorZ = cellZ * spacing + margin + cellRng.Next(0, jitterRange);

        if (worldX != anchorX || worldZ != anchorZ)
            return false;

        treeNoiseSettings.worldOffset = mapSeedOffset;
        float density = MyNoise.OctavePerlin(worldX, worldZ, treeNoiseSettings);
        if (density < treeDensityThreshold)
            return false;

        trunkHeight = cellRng.Next(minTrunkHeight, maxTrunkHeight + 1);
        return true;
    }

    private static int FloorDiv(int a, int b)
    {
        int q = a / b;
        if (a % b != 0 && ((a < 0) != (b < 0)))
            q--;
        return q;
    }

    private static bool InChunkBounds(ChunkData chunkData, int x, int y, int z)
    {
        return x >= 0 && x < chunkData.chunkSize
            && z >= 0 && z < chunkData.chunkSize
            && y >= 0 && y < chunkData.chunkHeight;
    }

    private static void TrySetLocalBlock(ChunkData chunkData, int x, int y, int z, BlockType block)
    {
        if (InChunkBounds(chunkData, x, y, z))
        {
            Chunk.SetBlock(chunkData, new Vector3Int(x, y, z), block);
        }
    }
}
