using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.Serialization.Formatters.Binary;
using UnityEngine;


[System.Serializable]
public class WorldSaveData
{
    // [ShowInInspector]  private HashSet<Vector3Int> _chunkCoordinates = new();
    // [ShowInInspector] private HashSet<Vector2Int> _chunkColumns = new();
    // [ShowInInspector] private Dictionary<Vector3Int, Chunk> _chunks = new();

    public SerializableVector3Int[] chunkCoordinates;
    public SerializableVector2Int[] chunkColumns;

    public SerializableVector3Int chunkSize;
    public SerializableVector3Int chunkCount;
    public SerializableVector3 playerPosition;

    public ChunkSaveData[] chunkSaveDatas;
    
    public WorldSaveData() { }

    public WorldSaveData(HashSet<Vector3Int> chunkCoordinates, HashSet<Vector2Int> chunkColumns, Dictionary<Vector3Int, Chunk> chunks, Vector3 playerPosition, Vector3Int chunkSize, Vector3Int chunkCount)
    {
        this.chunkSize = chunkSize;
        this.chunkCount = chunkCount;
        
        this.chunkCoordinates = new SerializableVector3Int[chunkCoordinates.Count];
        for (int i = 0; i < chunkCoordinates.Count; i++)
        {
            Vector3Int chunkCoord = chunkCoordinates.ElementAt(i);
            this.chunkCoordinates[i] = new SerializableVector3Int(chunkCoord.x, chunkCoord.y, chunkCoord.z);
        }
        
        this.chunkColumns = new SerializableVector2Int[chunkColumns.Count];
        for (int i = 0; i < chunkColumns.Count; i++)
        {
            Vector2Int chunkColumn = chunkColumns.ElementAt(i);
            this.chunkColumns[i] = new SerializableVector2Int(chunkColumn.x, chunkColumn.y);
        }
        
        this.playerPosition = new SerializableVector3(playerPosition.x, playerPosition.y, playerPosition.z);

        chunkSaveDatas = new ChunkSaveData[chunks.Count];
        
        for (int i = 0; i < chunks.Count; i++)
        {
            chunkSaveDatas[i] = new ChunkSaveData(chunks.ElementAt(i).Value.GetBlockTypes());
        }
    }
}

[Serializable]
public class ChunkSaveData
{
    public BlockType[] blockTypes;

    public ChunkSaveData() { }

    public ChunkSaveData(BlockType[] blockTypes)
    {
        this.blockTypes = blockTypes;
    }
}

public static class WorldSaver
{
    public static WorldSaveData WorldSaveData;
    static string savePath = $"{Application.persistentDataPath}/Saves";
    
    public static void Save(World world, string saveFileName = "")
    {
        DateTime now = DateTime.Now;
        if (string.IsNullOrEmpty(saveFileName))
        {
            string dateStr = $"{now.Year}_{now.Month}_{now.Day}___{now.Hour}_{now.Minute}_{now.Second}";
            saveFileName = $"{savePath}/World_{dateStr}.dat";
        }
        else
        {
            saveFileName = $"{savePath}/{saveFileName}.dat";
        }

        if (!File.Exists(savePath))
            Directory.CreateDirectory(savePath);

        BinaryFormatter binaryFormatter = new BinaryFormatter();
        FileStream file = File.Open(saveFileName, FileMode.OpenOrCreate, FileAccess.Write);
        WorldSaveData = new WorldSaveData(world.ChunkCoordinates, world.ChunkColumns, world.Chunks,
            world.Player.transform.position, world.ChunkSize, world.ChunkCount);
        
        binaryFormatter.Serialize(file, WorldSaveData);
        file.Close();
        Debug.LogError("World Saved to File : " + saveFileName);
    }

    public static WorldSaveData Load(string fileName)
    {
        string filePathFull = $"{savePath}/{fileName}";
        if (!File.Exists(filePathFull))
        {
            Debug.LogError("SaveFile does not exist. : " + filePathFull);
            return null;
        }

        BinaryFormatter binaryFormatter = new BinaryFormatter();
        FileStream file = File.Open(filePathFull, FileMode.Open, FileAccess.Read);
        WorldSaveData = (WorldSaveData) binaryFormatter.Deserialize(file);
        return WorldSaveData;
    }

    public static List<string> GetAllSaveFiles()
    {
        DirectoryInfo saveDirectory = new DirectoryInfo(savePath);
        return saveDirectory.GetFiles("*.dat").Select(x => x.Name.Split(".dat")[0]).ToList();
    }
}




/// <summary>
/// Since unity doesn't flag the Vector3 as serializable, we
/// need to create our own version. This one will automatically convert
/// between Vector3 and SerializableVector3
/// </summary>
[System.Serializable]
public struct SerializableVector3
{
    /// <summary>
    /// x component
    /// </summary>
    public float x;
     
    /// <summary>
    /// y component
    /// </summary>
    public float y;
     
    /// <summary>
    /// z component
    /// </summary>
    public float z;
     
    /// <summary>
    /// Constructor
    /// </summary>
    /// <param name="rX"></param>
    /// <param name="rY"></param>
    /// <param name="rZ"></param>
    public SerializableVector3(float rX, float rY, float rZ)
    {
        x = rX;
        y = rY;
        z = rZ;
    }
     
    /// <summary>
    /// Returns a string representation of the object
    /// </summary>
    /// <returns></returns>
    public override string ToString()
    {
        return String.Format("[{0}, {1}, {2}]", x, y, z);
    }
     
    /// <summary>
    /// Automatic conversion from SerializableVector3 to Vector3
    /// </summary>
    /// <param name="rValue"></param>
    /// <returns></returns>
    public static implicit operator Vector3(SerializableVector3 rValue)
    {
        return new Vector3(rValue.x, rValue.y, rValue.z);
    }
     
    /// <summary>
    /// Automatic conversion from Vector3 to SerializableVector3
    /// </summary>
    /// <param name="rValue"></param>
    /// <returns></returns>
    public static implicit operator SerializableVector3(Vector3 rValue)
    {
        return new SerializableVector3(rValue.x, rValue.y, rValue.z);
    }
}


/// <summary>
/// Since unity doesn't flag the Vector3 as serializable, we
/// need to create our own version. This one will automatically convert
/// between Vector3 and SerializableVector3
/// </summary>
[System.Serializable]
public struct SerializableVector3Int
{
    /// <summary>
    /// x component
    /// </summary>
    public int x;
     
    /// <summary>
    /// y component
    /// </summary>
    public int y;
     
    /// <summary>
    /// z component
    /// </summary>
    public int z;
     
    /// <summary>
    /// Constructor
    /// </summary>
    /// <param name="rX"></param>
    /// <param name="rY"></param>
    /// <param name="rZ"></param>
    public SerializableVector3Int(int rX, int rY, int rZ)
    {
        x = rX;
        y = rY;
        z = rZ;
    }
     
    /// <summary>
    /// Returns a string representation of the object
    /// </summary>
    /// <returns></returns>
    public override string ToString()
    {
        return String.Format("[{0}, {1}, {2}]", x, y, z);
    }
     
    /// <summary>
    /// Automatic conversion from SerializableVector3 to Vector3
    /// </summary>
    /// <param name="rValue"></param>
    /// <returns></returns>
    public static implicit operator Vector3Int(SerializableVector3Int rValue)
    {
        return new Vector3Int(rValue.x, rValue.y, rValue.z);
    }
     
    /// <summary>
    /// Automatic conversion from Vector3 to SerializableVector3
    /// </summary>
    /// <param name="rValue"></param>
    /// <returns></returns>
    public static implicit operator SerializableVector3Int(Vector3Int rValue)
    {
        return new SerializableVector3Int(rValue.x, rValue.y, rValue.z);
    }
}


/// <summary>
/// Since unity doesn't flag the Vector3 as serializable, we
/// need to create our own version. This one will automatically convert
/// between Vector3 and SerializableVector3
/// </summary>
[System.Serializable]
public struct SerializableVector2Int
{
    /// <summary>
    /// x component
    /// </summary>
    public int x;
     
    /// <summary>
    /// y component
    /// </summary>
    public int y;

    /// <summary>
    /// Constructor
    /// </summary>
    /// <param name="rX"></param>
    /// <param name="rY"></param>
    /// <param name="rZ"></param>
    public SerializableVector2Int(int rX, int rY)
    {
        x = rX;
        y = rY;
    }
     
    /// <summary>
    /// Returns a string representation of the object
    /// </summary>
    /// <returns></returns>
    public override string ToString()
    {
        return String.Format("[{0}, {1}]", x, y);
    }
     
    /// <summary>
    /// Automatic conversion from SerializableVector3 to Vector3
    /// </summary>
    /// <param name="rValue"></param>
    /// <returns></returns>
    public static implicit operator Vector2Int(SerializableVector2Int rValue)
    {
        return new Vector2Int(rValue.x, rValue.y);
    }
     
    /// <summary>
    /// Automatic conversion from Vector3 to SerializableVector3
    /// </summary>
    /// <param name="rValue"></param>
    /// <returns></returns>
    public static implicit operator SerializableVector2Int(Vector2Int rValue)
    {
        return new SerializableVector2Int(rValue.x, rValue.y);
    }
}
