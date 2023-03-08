using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Networking;
using System.IO;
using System.Runtime.Serialization.Formatters.Binary;

public class TileRequest : MonoBehaviour
{
    private PrecomputeChunk chunk;
    private string output = "old";
    private string requestUrl = "http://127.0.0.1:5000/download/";
    private UnityWebRequestAsyncOperation asyncOperation;
    
    // Start is called before the first frame update
    // void Start()
    // {
    //     StartCoroutine(GetRequest(, "info.txt"));
    //     Debug.Log(Application.persistentDataPath);
    // }

    public void Start()
    {
        StartCoroutine(GetRequest("info.txt"));
        
        Debug.Log(output);
        
    }

    public PrecomputeChunk GetChunk(string filename)
    {
        StartCoroutine(GetRequest(filename));

        return chunk;
    } 

    IEnumerator GetRequest(string filename)
    {
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
                //output = webRequest.downloadHandler.text;


                MemoryStream stream = new MemoryStream(webRequest.downloadHandler.data);

                System.Runtime.Serialization.Formatters.Binary.BinaryFormatter bf =
                    new System.Runtime.Serialization.Formatters.Binary.BinaryFormatter();

                chunk = (PrecomputeChunk)bf.Deserialize(stream);
            }

            yield return null;
        }

        // asyncOperation = UnityWebRequest.Get(requestUrl + filename).SendWebRequest();
        //
        // if (asyncOperation.isDone)
        // {
        //     Debug.Log(asyncOperation.webRequest.downloadHandler.text);
        // }
        // else
        // {
        //     Debug.Log("not finished");
        // }

    }

    
    void Update()
    {
        
    }
    
}
