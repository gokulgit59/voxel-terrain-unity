using System.IO;
using UnityEngine;

public static class SaveSystem
{
    private static string SaveFolder => Path.Combine(Application.persistentDataPath, "Saves", "World");

    public static void SaveChunk(ChunkData data)
    {
        if (data == null || !data.modifiedByThePlayer)
            return;

        Directory.CreateDirectory(SaveFolder);
        string path = GetChunkPath(data.worldPosition);

        using (BinaryWriter writer = new BinaryWriter(File.Open(path, FileMode.Create)))
        {
            writer.Write(data.blocks.Length);
            foreach (BlockType block in data.blocks)
            {
                writer.Write((int)block);
            }
        }
    }

    public static bool TryLoadChunk(ChunkData data)
    {
        string path = GetChunkPath(data.worldPosition);
        if (!File.Exists(path))
            return false;

        using (BinaryReader reader = new BinaryReader(File.Open(path, FileMode.Open)))
        {
            int length = reader.ReadInt32();
            if (length != data.blocks.Length)
                return false;

            for (int i = 0; i < length; i++)
            {
                data.blocks[i] = (BlockType)reader.ReadInt32();
            }
        }

        data.modifiedByThePlayer = true;
        return true;
    }

    private static string GetChunkPath(Vector3Int worldPosition)
    {
        return Path.Combine(SaveFolder, $"chunk_{worldPosition.x}_{worldPosition.y}_{worldPosition.z}.bin");
    }
}
