using System;
using System.Collections;
using System.Collections.Generic;
using Unity.Mathematics;
using UnityEngine;
using XNode;

[CreateNodeMenu("Gameplay/Place Mini-games")]
public class CalculatePOIRoute : SyncExtendedNode
{
    [Input] public List<GeoCoordinate> pointsOfInterest;
    [Input] public ElevationData elevationData;
    [Output] public List<GeoCoordinate> raceRoute;
    public override object GetValue(NodePort port)
    {
        if (port.fieldName == "raceRoute") return raceRoute;
        return null;
    }

    public override IEnumerator CalculateOutputs(Action<bool> callback)
    {
        raceRoute = GetInputValue("pointsOfInterest", pointsOfInterest);
        
        //instantiate some things where pois should be
        foreach (GeoCoordinate geoCoordinate in raceRoute)
        {
            Vector3 pos = getPositionFromGeoCoord(geoCoordinate);
            GameObject pointOfInterest = new GameObject("poi")
            {
                transform =
                {
                    position = pos,
                    rotation = quaternion.identity
                }
            };
        }
        
        callback.Invoke(true);
        yield break;
    }

    private Vector3 getPositionFromGeoCoord(GeoCoordinate geoCoordinate)
    {
        GenerateBuildingClassesNode node = CreateInstance<GenerateBuildingClassesNode>();
        Vector2 meterpoint = node.ConvertGeoCoordToMeters(geoCoordinate, elevationData.box);
        float height = (float)elevationData.SampleHeightFromPosition(new Vector3(meterpoint.x, 0, meterpoint.y)) + 100f;
        return new Vector3(meterpoint.x, height, meterpoint.y);
    }

    public override void Release()
    {
        pointsOfInterest = null;
        elevationData = null;
    }
}
