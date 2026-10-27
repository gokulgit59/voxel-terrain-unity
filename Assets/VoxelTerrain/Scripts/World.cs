using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

public class World : MonoBehaviour
{
    public int mapSizeInChunks = 6;
    public int chunkSize = 16, chunkHeight = 100;
    public int chunkDrawingRange = 8;

    public int chunkDataPerFrame = 8;
    public int chunksPerFrame = 4;

    public GameObject chunkPrefab;

    private Coroutine generationCoroutine;
    private readonly Queue<ChunkRenderer> chunkPool = new Queue<ChunkRenderer>();

    public TerrainGenerator terrainGenerator;
    public Vector2Int mapSeedOffset;

    //public Dictionary<Vector3Int, ChunkData> chunkDataDictionary = new Dictionary<Vector3Int, ChunkData>();
    //public Dictionary<Vector3Int, ChunkRenderer> chunkDictionary = new Dictionary<Vector3Int, ChunkRenderer>();

    public UnityEvent OnWorldCreated, OnNewChunksGenerated;

    [Tooltip("Build the world as soon as Play starts. Off = wait for the Regenerate button.")]
    public bool generateOnStart = true;

    public WorldData worldData { get; private set; }

    private void Awake()
    {
        worldData = new WorldData
        {
            chunkHeight = this.chunkHeight,
            chunkSize = this.chunkSize,
            chunkDataDictionary = new Dictionary<Vector3Int, ChunkData>(),
            chunkDictionary = new Dictionary<Vector3Int, ChunkRenderer>()
        };
    }

    private void Start()
    {
        if (generateOnStart)
            GenerateWorld();
    }

    public void GenerateWorld()
    {
        ClearWorld();
        GenerateWorld(Vector3Int.zero, null);
    }

    // Wipes everything currently generated so the next GenerateWorld() call rebuilds the
    // whole map from scratch with whatever values are set on the Inspector right now.
    // Without this, GenerateWorld() only creates positions that don't already exist, so
    // hitting "Regenerate" after tweaking noise/layer values had no visible effect.
    private void ClearWorld()
    {
        if (generationCoroutine != null)
        {
            StopCoroutine(generationCoroutine);
            generationCoroutine = null;
        }

        foreach (ChunkRenderer chunk in worldData.chunkDictionary.Values)
        {
            chunk.gameObject.SetActive(false);
            chunkPool.Enqueue(chunk);
        }

        worldData.chunkDictionary.Clear();
        worldData.chunkDataDictionary.Clear();
    }

    private void GenerateWorld(Vector3Int position, Action onComplete)
    {

        WorldGenerationData worldGenerationData = GetPositionsThatPlayerSees(position);

        foreach (Vector3Int pos in worldGenerationData.chunkPositionsToRemove)
        {
            WorldDataHelper.RemoveChunk(this, pos);
        }

        foreach (Vector3Int pos in worldGenerationData.chunkDataToRemove)
        {
            WorldDataHelper.RemoveChunkData(this, pos);
        }

        if (generationCoroutine != null)
        {
            StopCoroutine(generationCoroutine);
        }
        generationCoroutine = StartCoroutine(GenerateWorldRoutine(worldGenerationData, onComplete));
    }

    private IEnumerator GenerateWorldRoutine(WorldGenerationData worldGenerationData, Action onComplete)
    {
        int processed = 0;
        foreach (var pos in worldGenerationData.chunkDataPositionsToCreate)
        {
            ChunkData data = new ChunkData(chunkSize, chunkHeight, this, pos);
            if (!SaveSystem.TryLoadChunk(data))
            {
                data = terrainGenerator.GenerateChunkData(data, mapSeedOffset);
            }
            worldData.chunkDataDictionary.Add(pos, data);

            processed++;
            if (processed % chunkDataPerFrame == 0)
                yield return null;
        }

        processed = 0;
        foreach (var pos in worldGenerationData.chunkPositionsToCreate)
        {
            ChunkData data = worldData.chunkDataDictionary[pos];
            MeshData meshData = Chunk.GetChunkMeshData(data);
            ChunkRenderer chunkRenderer = GetPooledChunk(data.worldPosition);
            worldData.chunkDictionary.Add(data.worldPosition, chunkRenderer);
            chunkRenderer.InitializeChunk(data);
            chunkRenderer.UpdateChunk(meshData);

            processed++;
            if (processed % chunksPerFrame == 0)
                yield return null;
        }

        generationCoroutine = null;
        OnWorldCreated?.Invoke();
        onComplete?.Invoke();
    }

    internal bool SetBlock(RaycastHit hit, BlockType blockType)
    {
        ChunkRenderer chunk = hit.collider.GetComponent<ChunkRenderer>();
        if (chunk == null)
            return false;

        Vector3Int pos = GetBlockPos(hit);

        WorldDataHelper.SetBlock(chunk.ChunkData.worldReference, pos, blockType);
        chunk.ModifiedByThePlayer = true;

        if (Chunk.IsOnEdge(chunk.ChunkData, pos))
        {
            List<ChunkData> neighbourDataList = Chunk.GetEdgeNeighbourChunk(chunk.ChunkData, pos);
            foreach (ChunkData neighbourData in neighbourDataList)
            {
                //neighbourData.modifiedByThePlayer = true;
                ChunkRenderer chunkToUpdate = WorldDataHelper.GetChunk(neighbourData.worldReference, neighbourData.worldPosition);
                if (chunkToUpdate != null)
                    chunkToUpdate.UpdateChunk();
            }

        }

        chunk.UpdateChunk();
        return true;
    }

    private Vector3Int GetBlockPos(RaycastHit hit)
    {
        Vector3 pos = new Vector3(
             GetBlockPositionIn(hit.point.x, hit.normal.x),
             GetBlockPositionIn(hit.point.y, hit.normal.y),
             GetBlockPositionIn(hit.point.z, hit.normal.z)
             );

        return Vector3Int.RoundToInt(pos);
    }

    private float GetBlockPositionIn(float pos, float normal)
    {
        if (Mathf.Abs(pos % 1) == 0.5f)
        {
            pos -= (normal / 2);
        }


        return (float)pos;
    }

    internal void RemoveChunk(ChunkRenderer chunk)
    {
        chunk.gameObject.SetActive(false);
        chunkPool.Enqueue(chunk);
    }

    private ChunkRenderer GetPooledChunk(Vector3Int worldPosition)
    {
        ChunkRenderer chunkRenderer;
        if (chunkPool.Count > 0)
        {
            chunkRenderer = chunkPool.Dequeue();
            chunkRenderer.transform.position = worldPosition;
            chunkRenderer.gameObject.SetActive(true);
        }
        else
        {
            GameObject chunkObject = Instantiate(chunkPrefab, worldPosition, Quaternion.identity);
            chunkRenderer = chunkObject.GetComponent<ChunkRenderer>();
        }
        return chunkRenderer;
    }

    private WorldGenerationData GetPositionsThatPlayerSees(Vector3Int playerPosition)
    {
        List<Vector3Int> allChunkPositionsNeeded = WorldDataHelper.GetChunkPositionsAroundPlayer(this, playerPosition);
        List<Vector3Int> allChunkDataPositionsNeeded = WorldDataHelper.GetDataPositionsAroundPlayer(this, playerPosition);

        List<Vector3Int> chunkPositionsToCreate = WorldDataHelper.SelectPositonsToCreate(worldData, allChunkPositionsNeeded, playerPosition);
        List<Vector3Int> chunkDataPositionsToCreate = WorldDataHelper.SelectDataPositonsToCreate(worldData, allChunkDataPositionsNeeded, playerPosition);

        List<Vector3Int> chunkPositionsToRemove = WorldDataHelper.GetUnnededChunks(worldData, allChunkPositionsNeeded);
        List<Vector3Int> chunkDataToRemove = WorldDataHelper.GetUnnededData(worldData, allChunkDataPositionsNeeded);

        WorldGenerationData data = new WorldGenerationData
        {
            chunkPositionsToCreate = chunkPositionsToCreate,
            chunkDataPositionsToCreate = chunkDataPositionsToCreate,
            chunkPositionsToRemove = chunkPositionsToRemove,
            chunkDataToRemove = chunkDataToRemove,
            chunkPositionsToUpdate = new List<Vector3Int>()
        };
        return data;

    }

    public void SaveWorld()
    {
        foreach (ChunkData data in worldData.chunkDataDictionary.Values)
        {
            SaveSystem.SaveChunk(data);
        }
    }

    private void OnApplicationQuit()
    {
        SaveWorld();
    }

    internal void LoadAdditionalChunksRequest(GameObject player)
    {
        Debug.Log("Load more chunks");
        GenerateWorld(Vector3Int.RoundToInt(player.transform.position), () => OnNewChunksGenerated?.Invoke());
    }

    internal BlockType GetBlockFromChunkCoordinates(ChunkData chunkData, int x, int y, int z)
    {
        Vector3Int pos = Chunk.ChunkPositionFromBlockCoords(this, x, y, z);
        ChunkData containerChunk = null;

        worldData.chunkDataDictionary.TryGetValue(pos, out containerChunk);

        if (containerChunk == null)
            return BlockType.Nothing;
        Vector3Int blockInCHunkCoordinates = Chunk.GetBlockInChunkCoordinates(containerChunk, new Vector3Int(x, y, z));
        return Chunk.GetBlockFromChunkCoordinates(containerChunk, blockInCHunkCoordinates);
    }

    public struct WorldGenerationData
    {
        public List<Vector3Int> chunkPositionsToCreate;
        public List<Vector3Int> chunkDataPositionsToCreate;
        public List<Vector3Int> chunkPositionsToRemove;
        public List<Vector3Int> chunkDataToRemove;
        public List<Vector3Int> chunkPositionsToUpdate;
    }

    public struct WorldData
    {
        public Dictionary<Vector3Int, ChunkData> chunkDataDictionary;
        public Dictionary<Vector3Int, ChunkRenderer> chunkDictionary;
        public int chunkSize;
        public int chunkHeight;
    }
}