using UnityEngine;
using System.IO;
using System.Runtime.Serialization.Formatters.Binary;

public static class ChunkIO
{
    private const string PathToChunks = "/chunks/";
    public static PrecomputeChunk LoadIn(string filename)
    {
        System.Runtime.Serialization.Formatters.Binary.BinaryFormatter bf = new System.Runtime.Serialization.Formatters.Binary.BinaryFormatter();
        if (!Directory.Exists(Application.persistentDataPath + PathToChunks))
        {
            Directory.CreateDirectory(Application.persistentDataPath + PathToChunks);
        }
        System.IO.FileStream fs = new System.IO.FileStream(Application.persistentDataPath + PathToChunks + filename, System.IO.FileMode.Open, FileAccess.Read);
        PrecomputeChunk chunk = (PrecomputeChunk)bf.Deserialize(fs);
        fs.Flush();
        fs.Close();
        return chunk;
    }

    public static void Save(string filename, PrecomputeChunk chunk)
    {
        Debug.Log("Saving");
        System.Runtime.Serialization.Formatters.Binary.BinaryFormatter bf = new System.Runtime.Serialization.Formatters.Binary.BinaryFormatter();
        if (!Directory.Exists(Application.persistentDataPath + PathToChunks))
        {
            Directory.CreateDirectory(Application.persistentDataPath + PathToChunks);
        }
        Debug.Log("Serialized");
        System.IO.FileStream fs = new System.IO.FileStream(Application.persistentDataPath + PathToChunks + filename, System.IO.FileMode.Create, FileAccess.Write);
        bf.Serialize(fs, chunk);
        Debug.Log("Written");
        fs.Flush();
        fs.Close();
    }

    public static string GetFilePath(string filename)
    {
        return Application.persistentDataPath + PathToChunks + filename;
    }

    public static PrecomputeChunk LoadInASync(string filepath)
    {
        System.Runtime.Serialization.Formatters.Binary.BinaryFormatter bf = new System.Runtime.Serialization.Formatters.Binary.BinaryFormatter();
        System.IO.FileStream fs = new System.IO.FileStream(filepath, System.IO.FileMode.Open, FileAccess.Read);
        PrecomputeChunk chunk = (PrecomputeChunk)bf.Deserialize(fs);
        fs.Flush();
        fs.Close();
        return chunk;
    }
}

[System.Serializable]
public struct Vector3Serializable
{
    public float x, y, z;
    public Vector3Serializable(Vector2 v)
    {
        x = v.x;
        y = v.y;
        z = 0;
    }
    public Vector3Serializable(Vector3 v)
    {
        x = v.x;
        y = v.y;
        z = v.z;
    }

    public static implicit operator Vector3Serializable(Vector3 v)
    {
        return new Vector3Serializable(v);
    }
    public static implicit operator Vector3(Vector3Serializable v)
    {
        return new Vector3(v.x, v.y, v.z);
    }
    public static implicit operator Vector2(Vector3Serializable v)
    {
        return new Vector2(v.x, v.y);
    }

    public static implicit operator Vector3Serializable(Vector2 v)
    {
        return new Vector3Serializable(v);
    }
}