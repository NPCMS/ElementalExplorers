using System;
using System.Collections;
using System.Collections.Generic;
using Unity.Mathematics;
using Unity.Netcode;
using UnityEngine;
using XNode;

[CreateNodeMenu("Gameplay/Place Mini-games")]
public class CalculatePOIRoute : SyncExtendedNode
{
    [Input] public List<GeoCoordinate> pointsOfInterest;
    [Input] public ElevationData elevationData;
    [Input] public GameObject minigame;
    private ElevationData eData;
    [Output] public List<GeoCoordinate> raceRoute;
    public override object GetValue(NodePort port)
    {
        if (port.fieldName == "raceRoute") return raceRoute;
        return null;
    }

    public override IEnumerator CalculateOutputs(Action<bool> callback)
    {
        if (!MultiPlayerWrapper.isGameHost)
        {
            callback.Invoke(true);
            yield break;
        }
        
        raceRoute = GetInputValue("pointsOfInterest", pointsOfInterest);
        eData = GetInputValue("elevationData", elevationData);
        Debug.Log("________________________________________ " + raceRoute.Count);
        //instantiate some things where pois should be
        foreach (GeoCoordinate geoCoordinate in raceRoute)
        {
            Vector3 pos = getPositionFromGeoCoord(geoCoordinate);
            GameObject pointOfInterest = Instantiate(minigame, pos, quaternion.identity);
            pointOfInterest.GetComponent<NetworkObject>().Spawn();
        }
        
        callback.Invoke(true);
    }

    private Vector3 getPositionFromGeoCoord(GeoCoordinate geoCoordinate)
    {
        GenerateBuildingClassesNode node = CreateInstance<GenerateBuildingClassesNode>();
        Vector2 meterpoint = node.ConvertGeoCoordToMeters(geoCoordinate, eData.box);
        float height = (float)eData.SampleHeightFromPosition(new Vector3(meterpoint.x, 0, meterpoint.y)) + 300f;
        return new Vector3(meterpoint.x, height, meterpoint.y);
    }

    public override void Release()
    {
        pointsOfInterest = null;
        elevationData = null;
        eData = null;
    }
}
