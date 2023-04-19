using System;
using System.Collections;
using System.Collections.Generic;
using Newtonsoft.Json;
using UnityEngine;
using UnityEngine.Networking;
using Utils;
using XNode;

[CreateNodeMenu("Gameplay/Fetch points of interest backup")]
public class CalculatePOIsBackupNode : AsyncExtendedNode
{
    [Input] public List<GeoCoordinate> pointsOfInterestInput;
    [Input] public OSMBuildingData[] buildingDatas;
    [Input] public BuildifyFootprintList footprints;
    [Input] public GlobeBoundingBox boundingBox;
    [Input] public bool debug;
    private GlobeBoundingBox bbox;
    [Output] public List<GeoCoordinate> pointsOfInterestOutput;
    //public GlobeBoundingBox bbox;

    public override object GetValue(NodePort port)
    {
        if (port.fieldName == "pointsOfInterestOutput")
        {
            return pointsOfInterestOutput;
        }
        return null;
    }

    protected override void CalculateOutputsAsync(Action<bool> callback)
    {
        List<GeoCoordinate> pois = GetInputValue("pointsOfInterestInput", pointsOfInterestInput);
        OSMBuildingData[] buildings = GetInputValue("buildingDatas", buildingDatas);
        bbox = GetInputValue("boundingBox", boundingBox);
        BuildifyFootprintList buildingFootprints = GetInputValue("footprints", footprints);

        if (pois.Count > 0)
        {
            Debug.Log("no need for backup " + pois.Count);
            pointsOfInterestOutput = pois;
            callback.Invoke(true);
        }
        else
        {
            Debug.Log("backup required");

            //find the building with the largest volume.
            BuildifyFootprint biggestFootprint = null;
            float maxFootprintSize = 0f;
            foreach (var list in buildingFootprints.GetType().GetProperties())
            {
                var type = Nullable.GetUnderlyingType(list.PropertyType) ?? list.PropertyType;
                if (list.GetValue(buildingFootprints, null) is BuildifyFootprint[] footprints)
                {
                    biggestFootprint ??= footprints[0];
                    foreach (var footprint in footprints)
                    {
                        float footprintSum = 0f;
                        var faces = footprint.faces;
                        var verts = footprint.verts;
                        List<Vector3> vertices = new List<Vector3>();
                        int numTris = faces.Length;
                        int numVerts = verts[0].Length;
                        for (int i = 0; i < numVerts; i++)
                        {
                            vertices.Add(new Vector3(verts[i][0], verts[i][2], verts[i][1]));
                        }

                        for (int j = 0; j < numTris; j += 3)
                        {
                            footprintSum +=
                                BarycentricCoordinates.AreaOf3dTri(vertices[j], vertices[j + 1], vertices[j + 2]);
                        }

                        footprintSum *= footprint.height;
                        if (footprintSum > maxFootprintSize)
                        {
                            maxFootprintSize = footprintSum;
                            biggestFootprint = footprint;
                        }
                    }
                }
            }

            //ADD biggest footprint as a geocoordinate then done.
            if (biggestFootprint != null)
            {
                GeoCoordinate geoCoord =
                    bbox.MetersToGeoCoord(new Vector2(biggestFootprint.verts[0][0], biggestFootprint.verts[0][2]));
                pois.Add(geoCoord);
                Debug.Log("added a poi " + maxFootprintSize);
                callback.Invoke(true);
            }
            else
            {
                Debug.LogWarning("no point of interests found in backup just picking tallest building now...");
            }

            //find the tallest building
            OSMBuildingData maxHeightBuilding = null;
            if (buildings.Length > 0)
            {
                maxHeightBuilding = buildings[0];
            }

            float maxHeight = -1;
            foreach (OSMBuildingData buildingData in buildings)
            {
                if (buildingData.buildingHeight > maxHeight)
                {
                    maxHeight = buildingData.buildingHeight;
                    maxHeightBuilding = buildingData;
                }
            }

            if (maxHeightBuilding == null)
            {
                callback.Invoke(true);
                return;

            }
            pois.Add(convertBuildingDataToGeoCoordinate(maxHeightBuilding));
            callback.Invoke(true);
        }
    }

    private GeoCoordinate convertBuildingDataToGeoCoordinate(OSMBuildingData buildingData)
    {
        var center = buildingData.center;
        return bbox.MetersToGeoCoord(center);
    }
    
    protected override void ReleaseData()
    {
        pointsOfInterestInput = null;
        buildingDatas = null;
        footprints = null;
    }
}