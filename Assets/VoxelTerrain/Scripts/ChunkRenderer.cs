using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

[RequireComponent(typeof(MeshFilter))]
[RequireComponent(typeof(MeshRenderer))]
[RequireComponent(typeof(MeshCollider))]
public class ChunkRenderer : MonoBehaviour
{
    MeshFilter meshFilter;
    MeshCollider meshCollider;
    Mesh mesh;
    Mesh collisionMesh;

    // Scratch buffers shared by all chunks (meshes are built on the main thread, one at a time).
    static readonly List<Vector3> vertexBuffer = new List<Vector3>();
    static readonly List<Vector2> uvBuffer = new List<Vector2>();
    static readonly List<int> waterTriangleBuffer = new List<int>();
    static readonly List<Vector3> colliderVertexBuffer = new List<Vector3>();
    static readonly List<int> colliderTriangleBuffer = new List<int>();
    public bool showGizmo = false;

    public ChunkData ChunkData { get; private set; }

    public bool ModifiedByThePlayer
    {
        get
        {
            return ChunkData.modifiedByThePlayer;
        }
        set
        {
            ChunkData.modifiedByThePlayer = value;
        }
    }

    private void Awake()
    {
        meshFilter = GetComponent<MeshFilter>();
        meshCollider = GetComponent<MeshCollider>();
        mesh = meshFilter.mesh;
        // One collision mesh per chunk, reused on every rebuild (a new Mesh per update was never destroyed).
        collisionMesh = new Mesh { name = "Chunk Collider" };
    }

    private void OnDestroy()
    {
        Destroy(mesh);
        Destroy(collisionMesh);
    }

    public void InitializeChunk(ChunkData data)
    {
        this.ChunkData = data;
    }

    private void RenderMesh(MeshData meshData)
    {
        mesh.Clear();

        // Land and water share one vertex buffer: land first, then water, with the water indices offset.
        // Shared buffers are reused across rebuilds so uploading a chunk allocates nothing.
        int landVertexCount = meshData.vertices.Count;
        vertexBuffer.Clear();
        vertexBuffer.AddRange(meshData.vertices);
        vertexBuffer.AddRange(meshData.waterMesh.vertices);
        uvBuffer.Clear();
        uvBuffer.AddRange(meshData.uv);
        uvBuffer.AddRange(meshData.waterMesh.uv);
        waterTriangleBuffer.Clear();
        foreach (int index in meshData.waterMesh.triangles)
            waterTriangleBuffer.Add(index + landVertexCount);

        mesh.subMeshCount = 2;
        mesh.SetVertices(vertexBuffer);
        mesh.SetTriangles(meshData.triangles, 0);
        mesh.SetTriangles(waterTriangleBuffer, 1);
        mesh.SetUVs(0, uvBuffer);
        mesh.RecalculateNormals();

        meshCollider.sharedMesh = null;

        colliderVertexBuffer.Clear();
        colliderTriangleBuffer.Clear();
        GreedyColliderMesher.BuildColliderMesh(ChunkData, colliderVertexBuffer, colliderTriangleBuffer);

        collisionMesh.Clear();
        collisionMesh.indexFormat = colliderVertexBuffer.Count > 65000
            ? UnityEngine.Rendering.IndexFormat.UInt32
            : UnityEngine.Rendering.IndexFormat.UInt16;
        collisionMesh.SetVertices(colliderVertexBuffer);
        collisionMesh.SetTriangles(colliderTriangleBuffer, 0);

        meshCollider.sharedMesh = collisionMesh;
    }

    public void UpdateChunk()
    {
        RenderMesh(Chunk.GetChunkMeshData(ChunkData));
    }

    public void UpdateChunk(MeshData data)
    {
        RenderMesh(data);
    }

#if UNITY_EDITOR
    private void OnDrawGizmos()
    {
        if (showGizmo)
        {
            if (Application.isPlaying && ChunkData != null)
            {
                if (Selection.activeObject == gameObject)
                    Gizmos.color = new Color(0, 1, 0, 0.4f);
                else
                    Gizmos.color = new Color(1, 0, 1, 0.4f);

                Gizmos.DrawCube(transform.position + new Vector3(ChunkData.chunkSize / 2f, ChunkData.chunkHeight / 2f, ChunkData.chunkSize / 2f), new Vector3(ChunkData.chunkSize, ChunkData.chunkHeight, ChunkData.chunkSize));
            }
        }
    }
#endif
}