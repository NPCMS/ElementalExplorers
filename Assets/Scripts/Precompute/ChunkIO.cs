using UnityEngine;
using System.IO;
using System.Runtime.Serialization.Formatters.Binary;
using Newtonsoft.Json;

public static class ChunkIO
{
    private const string PathToChunks = "/chunks/";
    public static PrecomputeChunk LoadIn(string filepath)
    {
        System.Runtime.Serialization.Formatters.Binary.BinaryFormatter bf = new System.Runtime.Serialization.Formatters.Binary.BinaryFormatter();
        //if (!Directory.Exists(Application.persistentDataPath + PathToChunks))
        //{
        //    Directory.CreateDirectory(Application.persistentDataPath + PathToChunks);
        //}
        System.IO.FileStream fs = new System.IO.FileStream(filepath, System.IO.FileMode.Open, FileAccess.Read);
        PrecomputeChunk chunk = (PrecomputeChunk)bf.Deserialize(fs);
        fs.Flush();
        fs.Close();
        return chunk;
    }

    public static void Save(string filename, PrecomputeChunk chunk)
    {
        System.Runtime.Serialization.Formatters.Binary.BinaryFormatter bf = new System.Runtime.Serialization.Formatters.Binary.BinaryFormatter();
        if (!Directory.Exists(Application.persistentDataPath + PathToChunks))
        {
            Directory.CreateDirectory(Application.persistentDataPath + PathToChunks);
        }
        System.IO.FileStream fs = new System.IO.FileStream(Application.persistentDataPath + PathToChunks + filename, System.IO.FileMode.Create, FileAccess.Write);
        bf.Serialize(fs, chunk);
        fs.Flush();
        fs.Close();
    }

    public static void SaveJSON(string filename, PrecomputeChunk chunk)
    {
        if (!Directory.Exists(Application.persistentDataPath + PathToChunks))
        {
            Directory.CreateDirectory(Application.persistentDataPath + PathToChunks);
        }
        System.IO.FileStream fs = new System.IO.FileStream(Application.persistentDataPath + PathToChunks + filename, System.IO.FileMode.Create, FileAccess.Write);
        using (StreamWriter writer = new StreamWriter(fs))
        {
            writer.Write(JsonUtility.ToJson(chunk));

        }
        fs.Close();
    }


    public static string GetFilePath(string filename)
    {
        return Application.persistentDataPath + PathToChunks + filename;
    }

    public static PrecomputeChunk LoadInJSON(string filepath)
    {
        System.IO.FileStream fs = new System.IO.FileStream(filepath, System.IO.FileMode.Open, FileAccess.Read);
        PrecomputeChunk chunk;
        using (StreamReader reader = new StreamReader(fs))
        {
            chunk = JsonUtility.FromJson<PrecomputeChunk>(reader.ReadToEnd());
        }
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

[System.Serializable]
public struct Vector2Serializable
{
    public float x, y;
    public Vector2Serializable(Vector2 v)
    {
        x = v.x;
        y = v.y;
    }

    public static implicit operator Vector2Serializable(Vector2 v)
    {
        return new Vector2Serializable(v);
    }
    public static implicit operator Vector2(Vector2Serializable v)
    {
        return new Vector2(v.x, v.y);
    }
}

[System.Serializable]
public class RoadNetworkNodeSerialised
{
    public Vector2Serializable location;
    public readonly ulong id;

    public RoadNetworkNodeSerialised(Vector2 location, ulong id)
    {
        this.location = location;
        this.id = id;
    }

    public bool Equals(RoadNetworkNode other)
    {
        return id == other.id;
    }

    public override bool Equals(object obj)
    {
        return obj is RoadNetworkNode other && Equals(other);
    }

    public override int GetHashCode()
    {
        return id.GetHashCode();
    }
}

[System.Serializable]
public struct RoadNetworkEdgeSerialised
{
    public float length;
    public RoadType type;
    public Vector2Serializable[] edgePoints;

    public RoadNetworkEdgeSerialised(float length, RoadType type, Vector2[] edgePoints)
    {
        this.length = length;
        this.type = type;
        this.edgePoints = new Vector2Serializable[edgePoints.Length];
        for (int i = 0; i < this.edgePoints.Length; i++)
        {
            this.edgePoints[i] = edgePoints[i];
        }
    }
}