using System;
using ProceduralPipelineNodes.Nodes.Buildings;
using UnityEngine;
using UnityEngine.Networking;
using XNode;

namespace ProceduralPipelineNodes.Nodes.Roads
{
    [CreateNodeMenu("Legacy/Roads/OSM Roads Ways Data")]
    public class OSMRoadsWaysDataNode : ExtendedNode
    {
        [Input] public GlobeBoundingBox boundingBox;
        [Input] public int timeout;
        [Input] public int maxSize;
        [Input] public bool debug;
        [Output] public OSMRoadWay[] wayArray;
        // Use this for initialization
        protected override void Init()
        {
            base.Init();

        }

        // Return the correct value of an output port when requested
        public override object GetValue(NodePort port)
        {
            if (port.fieldName == "wayArray")
            {
                return wayArray;
            }
            else
            {
                return null;
            }
        }

        public override void CalculateOutputs(Action<bool> callback)
        {
            GlobeBoundingBox actualBoundingBox = GetInputValue("boundingBox", boundingBox);
            int actualTimeout = GetInputValue("timeout", timeout);
            int actualMaxSize = GetInputValue("maxSize", maxSize);
            sendRequest(actualBoundingBox, actualTimeout, actualMaxSize, callback);

        }

        public void sendRequest(GlobeBoundingBox boundingBox, int timeout, int maxSize, Action<bool> callback)
        {
            string endpoint = "https://overpass.kumi.systems/api/interpreter/?";
            string query = "data=[out:json][timeout:" + timeout + "][maxsize:" + maxSize + "];way[highway](" + boundingBox.south + "," + boundingBox.west + "," +
                           boundingBox.north + "," + boundingBox.east + ");out body;>;out skel qt;";
            string sendURL = endpoint + query;


            UnityWebRequest request = UnityWebRequest.Get(sendURL);
            UnityWebRequestAsyncOperation operation = request.SendWebRequest();
            operation.completed += (AsyncOperation operation) =>
            {
                if (request.result != UnityWebRequest.Result.Success)
                {
                    Debug.Log(request.error);
                    callback.Invoke(false);
                }
                else
                {
                    OSMRoadWayContainer result = JsonUtility.FromJson<OSMRoadWayContainer>(request.downloadHandler.text);

                    wayArray = result.elements;
                    if(debug)
                    {
                        foreach(OSMRoadWay way in wayArray)
                        {
					
                    
                            Debug.Log("id :- " + way.id);
                            Debug.Log(way.tags);
                            Debug.Log("num of nodes:- " + way.nodes.Length);
                    
                        }
                    }
                    Debug.Log("this many ways from server :- " + wayArray.Length);
				
                    callback.Invoke(true);
                }
                request.Dispose();
            };
        }

        public override void Release()
        {
            base.Release();
            wayArray = null;
        }
    }

    [System.Serializable]
    public class OSMRoadWayContainer
    {
        public OSMRoadWay[] elements;
    }

    [System.Serializable]
    public class OSMRoadWay
    {
        public ulong id;
        public ulong[] nodes;
        public OSMTags tags;
    }
}