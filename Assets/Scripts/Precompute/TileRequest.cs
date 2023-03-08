using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Networking;
using System.IO;
using System.Runtime.Serialization.Formatters.Binary;

public class TileRequest : MonoBehaviour
{ 
    
    private PrecomputeChunk chunk;
    private string[] chunks;
    

    public string[] GetAvaliableChunks()
    {
        StartCoroutine(GetAvailableChunksRequest());

        return chunks;
    }

    private IEnumerator GetAvailableChunksRequest()
    {
        string requestUrl = "http://127.0.0.1:5000/download/list/";
        using (UnityWebRequest webRequest = UnityWebRequest.Get(requestUrl + "available_chunks.txt"))
        {
            webRequest.SendWebRequest();
            while (webRequest.result == UnityWebRequest.Result.InProgress)
            {
            }

            if (webRequest.result != UnityWebRequest.Result.Success)
            {
                Debug.LogError(webRequest.error);
                
            }
            else
            {
                Debug.Log("success");
                
                chunks = webRequest.downloadHandler.text.Split('\n');
            }

            yield return null;
        }
    }

    public PrecomputeChunk GetChunk(string filename)
    {
        StartCoroutine(GetChunkRequest(filename));

        return chunk;
    } 

    private IEnumerator GetChunkRequest(string filename)
    {
        string requestUrl = "http://127.0.0.1:5000/download/chunks/";
        using (UnityWebRequest webRequest = UnityWebRequest.Get(requestUrl + filename))
        {
            webRequest.SendWebRequest();
            while (webRequest.result == UnityWebRequest.Result.InProgress)
            {
            }

            if (webRequest.result != UnityWebRequest.Result.Success)
            {
                Debug.LogError(webRequest.error);
                
            }
            else
            {
                Debug.Log("success");


                MemoryStream stream = new MemoryStream(webRequest.downloadHandler.data);
                
                System.Runtime.Serialization.Formatters.Binary.BinaryFormatter bf =
                    new System.Runtime.Serialization.Formatters.Binary.BinaryFormatter();
                
                chunk = (PrecomputeChunk)bf.Deserialize(stream);
            }

            yield return null;
        }

    }

    
    void Update()
    {
        
    }
    
}
