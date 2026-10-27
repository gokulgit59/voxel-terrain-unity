using UnityEngine;

// Plain data holder, never attached to a GameObject — must NOT derive from MonoBehaviour.
// `new ChunkData(...)` is called all over the codebase, and a MonoBehaviour created via
// `new` becomes a "fake null" (Unity's == override reports it as null even though it holds
// real data), which silently broke neighbor/chunk lookups and caused missing faces and
// disconnected floating chunk fragments.
public class ChunkData
{
    public BlockType[] blocks;
    public int chunkSize = 16;
    public int chunkHeight = 100;
    public World worldReference;
    public Vector3Int worldPosition;

    public bool modifiedByThePlayer = false;
    
    public ChunkData(int chunkSize, int chunkHeight, World world, Vector3Int worldPosition)
    {
        this.chunkHeight = chunkHeight;
        this.chunkSize = chunkSize;
        this.worldReference = world;
        this.worldPosition = worldPosition;
        blocks = new BlockType[chunkSize * chunkHeight * chunkSize];
    }
}
