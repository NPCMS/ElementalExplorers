using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.IO;


public static class ChunkIO
{
    private const string PathToChunks = "/chunks/";
    public static PrecomputeChunk LoadIn(string filename)
    {
        if(!System.IO.File.Exists(Application.dataPath + "meshFile.dat"))
        {
            Debug.LogError("meshFile.dat file does not exist.");
            return null;
        }
        System.Runtime.Serialization.Formatters.Binary.BinaryFormatter bf = new System.Runtime.Serialization.Formatters.Binary.BinaryFormatter();
        System.IO.FileStream fs = new System.IO.FileStream(Application.dataPath + PathToChunks + filename, System.IO.FileMode.Open);
        PrecomputeChunk chunk = (PrecomputeChunk)bf.Deserialize(fs);
        fs.Flush();
        fs.Close();
        return chunk;
    }

    public static void Save(string filename, PrecomputeChunk chunk)
    {
        System.Runtime.Serialization.Formatters.Binary.BinaryFormatter bf = new System.Runtime.Serialization.Formatters.Binary.BinaryFormatter();
        System.IO.FileStream fs = new System.IO.FileStream(Application.dataPath + PathToChunks + filename, System.IO.FileMode.Create);
        bf.Serialize(fs, chunk);
        fs.Flush();
        fs.Close();
    }
}
