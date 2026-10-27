using System.Collections.Generic;
using UnityEngine;

// Builds a greedy-merged collision mesh for a chunk: adjacent solid, collidable faces on the
// same plane are merged into single rectangles instead of one quad per block. This only affects
// physics geometry (no UVs), so it's safe to merge without any texture-stretch risk.
public static class GreedyColliderMesher
{
    public static void BuildColliderMesh(ChunkData chunkData, List<Vector3> vertices, List<int> triangles)
    {
        int[] dims = { chunkData.chunkSize, chunkData.chunkHeight, chunkData.chunkSize };

        // Every lookup below stays inside this chunk, so resolve "solid and collidable" once per voxel
        // straight from the block array instead of going through the world lookup for each test.
        bool[] collidableType = CollidableTypes();
        BlockType[] blocks = chunkData.blocks;
        bool[] solid = new bool[blocks.Length];
        for (int i = 0; i < blocks.Length; i++)
            solid[i] = collidableType[(int)blocks[i]];
        int strideY = chunkData.chunkSize;
        int strideZ = chunkData.chunkSize * chunkData.chunkHeight;
        int[] stride = { 1, strideY, strideZ };
        int[] du = new int[3];
        int[] dv = new int[3];

        for (int d = 0; d < 3; d++)
        {
            int u = (d + 1) % 3;
            int v = (d + 2) % 3;

            int[] x = new int[3];
            int[] q = new int[3];
            q[d] = 1;

            int[] mask = new int[dims[u] * dims[v]];

            for (x[d] = -1; x[d] < dims[d];)
            {
                int n = 0;
                for (x[v] = 0; x[v] < dims[v]; x[v]++)
                {
                    for (x[u] = 0; x[u] < dims[u]; x[u]++)
                    {
                        int index = x[0] + strideY * x[1] + strideZ * x[2];
                        bool blockCurrent = x[d] >= 0 && solid[index];
                        bool blockCompare = x[d] < dims[d] - 1 && solid[index + stride[d]];

                        if (blockCurrent == blockCompare)
                            mask[n] = 0;
                        else if (blockCurrent)
                            mask[n] = 1;
                        else
                            mask[n] = -1;

                        n++;
                    }
                }

                x[d]++;

                n = 0;
                for (int j = 0; j < dims[v]; j++)
                {
                    for (int i = 0; i < dims[u];)
                    {
                        int c = mask[n];
                        if (c != 0)
                        {
                            int w = 1;
                            while (i + w < dims[u] && mask[n + w] == c)
                                w++;

                            int h = 1;
                            bool done = false;
                            for (; j + h < dims[v]; h++)
                            {
                                for (int k = 0; k < w; k++)
                                {
                                    if (mask[n + k + h * dims[u]] != c)
                                    {
                                        done = true;
                                        break;
                                    }
                                }
                                if (done)
                                    break;
                            }

                            x[u] = i;
                            x[v] = j;

                            du[0] = du[1] = du[2] = 0;
                            dv[0] = dv[1] = dv[2] = 0;
                            du[u] = w;
                            dv[v] = h;

                            Vector3 baseVertex = new Vector3(x[0] - 0.5f, x[1] - 0.5f, x[2] - 0.5f);
                            Vector3 duVec = new Vector3(du[0], du[1], du[2]);
                            Vector3 dvVec = new Vector3(dv[0], dv[1], dv[2]);

                            int vCount = vertices.Count;

                            if (c > 0)
                            {
                                vertices.Add(baseVertex);
                                vertices.Add(baseVertex + duVec);
                                vertices.Add(baseVertex + duVec + dvVec);
                                vertices.Add(baseVertex + dvVec);
                            }
                            else
                            {
                                vertices.Add(baseVertex);
                                vertices.Add(baseVertex + dvVec);
                                vertices.Add(baseVertex + duVec + dvVec);
                                vertices.Add(baseVertex + duVec);
                            }

                            triangles.Add(vCount);
                            triangles.Add(vCount + 1);
                            triangles.Add(vCount + 2);
                            triangles.Add(vCount);
                            triangles.Add(vCount + 2);
                            triangles.Add(vCount + 3);

                            for (int l = 0; l < h; l++)
                            {
                                for (int k = 0; k < w; k++)
                                {
                                    mask[n + k + l * dims[u]] = 0;
                                }
                            }

                            i += w;
                            n += w;
                        }
                        else
                        {
                            i++;
                            n++;
                        }
                    }
                }
            }
        }
    }

    // Indexed by (int)BlockType: true when the block is solid and generates a collider.
    private static bool[] CollidableTypes()
    {
        var values = (BlockType[])System.Enum.GetValues(typeof(BlockType));
        bool[] result = new bool[values.Length];
        foreach (BlockType blockType in values)
        {
            if (blockType == BlockType.Nothing)
                continue;
            if (BlockDataManager.blockTextureDataDictionary.TryGetValue(blockType, out TextureData data))
                result[(int)blockType] = data.isSolid && data.generatesCollider;
        }
        return result;
    }
}
