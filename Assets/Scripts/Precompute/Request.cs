using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Networking;
using System.IO;

public class Request : MonoBehaviour
{
    // Start is called before the first frame update
    string requestUrl = "http://127.0.0.1:5000/";

   

    IEnumerator GetRequest(string name)
    {
        yield return null;
        PrecomputeChunk chunk;
        using (UnityWebRequest webRequest = UnityWebRequest.Get(requestUrl + name))
        {
            webRequest.SendWebRequest();
            if(webRequest.isNetworkError)
            {
                Debug.Log("Error" + webRequest.error);
            }
            else
            {
                
                // System.Runtime.Serialization.Formatters.Binary.BinaryFormatter bf = new System.Runtime.Serialization.Formatters.Binary.BinaryFormatter();

                // chunk = (PrecomputeChunk) bf.Deserialize(new MemoryStream(webRequest.downloadHandler.data));

                // yield return chunk;
            }
        }


    }

  
}
