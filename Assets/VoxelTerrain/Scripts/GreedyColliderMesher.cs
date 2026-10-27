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
                        bool blockCurrent = x[d] >= 0 && IsCollidable(chunkData, x[0], x[1], x[2]);
                        bool blockCompare = x[d] < dims[d] - 1 && IsCollidable(chunkData, x[0] + q[0], x[1] + q[1], x[2] + q[2]);

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

                            int[] du = new int[3];
                            int[] dv = new int[3];
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

    private static bool IsCollidable(ChunkData chunkData, int x, int y, int z)
    {
        BlockType blockType = Chunk.GetBlockFromChunkCoordinates(chunkData, x, y, z);
        if (blockType == BlockType.Nothing)
            return false;
        if (!BlockDataManager.blockTextureDataDictionary.TryGetValue(blockType, out TextureData data))
            return false;
        return data.isSolid && data.generatesCollider;
    }
}
