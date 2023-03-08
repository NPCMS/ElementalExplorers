using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Networking;

public class TileRequest : MonoBehaviour
{
    // Start is called before the first frame update
    void Start()
    {
        StartCoroutine(GetRequest("http://127.0.0.1:5000/", "download/info.txt"));
    }

    IEnumerator GetRequest(string uri, string filename)
    {
        using (UnityWebRequest webRequest = UnityWebRequest.Get(uri + filename))
        {
            yield return webRequest.SendWebRequest();
            if (webRequest.result != UnityWebRequest.Result.Success)
            {
                Debug.LogError(webRequest.error);
            }
            else
            {
                Debug.Log("success");
                Debug.Log(webRequest.downloadHandler.text);
            }
        }
    }
}
