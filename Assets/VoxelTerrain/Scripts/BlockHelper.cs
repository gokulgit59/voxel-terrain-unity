using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public static class BlockHelper
{
    private static Direction[] directions =
    {
        Direction.backwards,
        Direction.down,
        Direction.foreward,
        Direction.left,
        Direction.right,
        Direction.up
    };

    // Same order as directions.
    private static readonly Vector3Int[] directionVectors =
    {
        Vector3Int.back,
        Vector3Int.down,
        Vector3Int.forward,
        Vector3Int.left,
        Vector3Int.right,
        Vector3Int.up
    };

    // Indexed by (int)BlockType. Built once per chunk mesh so face tests don't hit the dictionary.
    public static bool[] BuildSolidTable()
    {
        var values = (BlockType[])System.Enum.GetValues(typeof(BlockType));
        bool[] solid = new bool[values.Length];
        foreach (BlockType blockType in values)
        {
            if (BlockDataManager.blockTextureDataDictionary.TryGetValue(blockType, out TextureData data))
                solid[(int)blockType] = data.isSolid;
        }
        return solid;
    }

    public static MeshData GetMeshData
        (ChunkData chunk, int x, int y, int z, MeshData meshData, BlockType blockType)
    {
        return GetMeshData(chunk, x, y, z, meshData, blockType, BuildSolidTable());
    }

    public static MeshData GetMeshData
        (ChunkData chunk, int x, int y, int z, MeshData meshData, BlockType blockType, bool[] isSolid)
    {
        if (blockType == BlockType.Air || blockType == BlockType.Nothing)
            return meshData;

        for (int i = 0; i < directions.Length; i++)
        {
            Direction direction = directions[i];
            Vector3Int offset = directionVectors[i];
            int nx = x + offset.x, ny = y + offset.y, nz = z + offset.z;

            // Neighbours inside the chunk are read straight from the block array; only edge faces go through the world.
            BlockType neighbourBlockType;
            if (nx >= 0 && nx < chunk.chunkSize && ny >= 0 && ny < chunk.chunkHeight && nz >= 0 && nz < chunk.chunkSize)
                neighbourBlockType = chunk.blocks[nx + chunk.chunkSize * ny + chunk.chunkSize * chunk.chunkHeight * nz];
            else
                neighbourBlockType = Chunk.GetBlockFromChunkCoordinates(chunk, nx, ny, nz);

            if (neighbourBlockType != BlockType.Nothing && isSolid[(int)neighbourBlockType] == false)
            {

                if (blockType == BlockType.Water)
                {
                    if (neighbourBlockType == BlockType.Air)
                        meshData.waterMesh = GetFaceDataIn(direction, chunk, x, y, z, meshData.waterMesh, blockType);
                }
                else
                {
                    meshData = GetFaceDataIn(direction, chunk, x, y, z, meshData, blockType);
                }

            }
        }

        return meshData;
    }

    public static MeshData GetFaceDataIn(Direction direction, ChunkData chunk, int x, int y, int z, MeshData meshData, BlockType blockType)
    {
        GetFaceVertices(direction, x, y, z, meshData, blockType);
        meshData.AddQuadTriangles();
        AddFaceUVs(meshData.uv, direction, blockType);


        return meshData;
    }

    public static void GetFaceVertices(Direction direction, int x, int y, int z, MeshData meshData, BlockType blockType)
    {
        //order of vertices matters for the normals and how we render the mesh
        switch (direction)
        {
            case Direction.backwards:
                meshData.AddVertex(new Vector3(x - 0.5f, y - 0.5f, z - 0.5f));
                meshData.AddVertex(new Vector3(x - 0.5f, y + 0.5f, z - 0.5f));
                meshData.AddVertex(new Vector3(x + 0.5f, y + 0.5f, z - 0.5f));
                meshData.AddVertex(new Vector3(x + 0.5f, y - 0.5f, z - 0.5f));
                break;
            case Direction.foreward:
                meshData.AddVertex(new Vector3(x + 0.5f, y - 0.5f, z + 0.5f));
                meshData.AddVertex(new Vector3(x + 0.5f, y + 0.5f, z + 0.5f));
                meshData.AddVertex(new Vector3(x - 0.5f, y + 0.5f, z + 0.5f));
                meshData.AddVertex(new Vector3(x - 0.5f, y - 0.5f, z + 0.5f));
                break;
            case Direction.left:
                meshData.AddVertex(new Vector3(x - 0.5f, y - 0.5f, z + 0.5f));
                meshData.AddVertex(new Vector3(x - 0.5f, y + 0.5f, z + 0.5f));
                meshData.AddVertex(new Vector3(x - 0.5f, y + 0.5f, z - 0.5f));
                meshData.AddVertex(new Vector3(x - 0.5f, y - 0.5f, z - 0.5f));
                break;

            case Direction.right:
                meshData.AddVertex(new Vector3(x + 0.5f, y - 0.5f, z - 0.5f));
                meshData.AddVertex(new Vector3(x + 0.5f, y + 0.5f, z - 0.5f));
                meshData.AddVertex(new Vector3(x + 0.5f, y + 0.5f, z + 0.5f));
                meshData.AddVertex(new Vector3(x + 0.5f, y - 0.5f, z + 0.5f));
                break;
            case Direction.down:
                meshData.AddVertex(new Vector3(x - 0.5f, y - 0.5f, z - 0.5f));
                meshData.AddVertex(new Vector3(x + 0.5f, y - 0.5f, z - 0.5f));
                meshData.AddVertex(new Vector3(x + 0.5f, y - 0.5f, z + 0.5f));
                meshData.AddVertex(new Vector3(x - 0.5f, y - 0.5f, z + 0.5f));
                break;
            case Direction.up:
                meshData.AddVertex(new Vector3(x - 0.5f, y + 0.5f, z + 0.5f));
                meshData.AddVertex(new Vector3(x + 0.5f, y + 0.5f, z + 0.5f));
                meshData.AddVertex(new Vector3(x + 0.5f, y + 0.5f, z - 0.5f));
                meshData.AddVertex(new Vector3(x - 0.5f, y + 0.5f, z - 0.5f));
                break;
            default:
                break;
        }
    }

    // Same four UVs as FaceUVs, written straight into the list (no array per face).
    public static void AddFaceUVs(List<Vector2> uvs, Direction direction, BlockType blockType)
    {
        var tilePos = TexturePosition(direction, blockType);
        float left = BlockDataManager.tileSizeX * tilePos.x + BlockDataManager.textureOffset;
        float right = BlockDataManager.tileSizeX * tilePos.x + BlockDataManager.tileSizeX - BlockDataManager.textureOffset;
        float bottom = BlockDataManager.tileSizeY * tilePos.y + BlockDataManager.textureOffset;
        float top = BlockDataManager.tileSizeY * tilePos.y + BlockDataManager.tileSizeY - BlockDataManager.textureOffset;
        uvs.Add(new Vector2(right, bottom));
        uvs.Add(new Vector2(right, top));
        uvs.Add(new Vector2(left, top));
        uvs.Add(new Vector2(left, bottom));
    }

    public static Vector2[] FaceUVs(Direction direction, BlockType blockType)
    {
        Vector2[] UVs = new Vector2[4];
        var tilePos = TexturePosition(direction, blockType);

        UVs[0] = new Vector2(BlockDataManager.tileSizeX * tilePos.x + BlockDataManager.tileSizeX - BlockDataManager.textureOffset,
            BlockDataManager.tileSizeY * tilePos.y + BlockDataManager.textureOffset);

        UVs[1] = new Vector2(BlockDataManager.tileSizeX * tilePos.x + BlockDataManager.tileSizeX - BlockDataManager.textureOffset,
            BlockDataManager.tileSizeY * tilePos.y + BlockDataManager.tileSizeY - BlockDataManager.textureOffset);

        UVs[2] = new Vector2(BlockDataManager.tileSizeX * tilePos.x + BlockDataManager.textureOffset,
            BlockDataManager.tileSizeY * tilePos.y + BlockDataManager.tileSizeY - BlockDataManager.textureOffset);

        UVs[3] = new Vector2(BlockDataManager.tileSizeX * tilePos.x + BlockDataManager.textureOffset,
            BlockDataManager.tileSizeY * tilePos.y + BlockDataManager.textureOffset);

        return UVs;
    }

    public static Vector2Int TexturePosition(Direction direction, BlockType blockType)
    {
        return direction switch
        {
            Direction.up => BlockDataManager.blockTextureDataDictionary[blockType].up,
            Direction.down => BlockDataManager.blockTextureDataDictionary[blockType].down,
            _ => BlockDataManager.blockTextureDataDictionary[blockType].side
        };
    }
}