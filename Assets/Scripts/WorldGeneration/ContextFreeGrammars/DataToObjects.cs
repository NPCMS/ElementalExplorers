using System.Collections.Generic;
using UnityEngine;

public static class DataToObjects
{
   


    public static bool TryCreateObjectOnWay(GameObject parent, OSMBuildingData building, string obj)
    {
        Vector2[] way = building.footprint.ToArray();
        //way = MakeAntiClockwise(way);
        bool success = true;
        try
        {
            success = CreateObj(parent, building, way, obj);
        }
        catch (System.Exception)
        {
            success = false;
        }
        //Mesh mesh = new Mesh();
        //mesh.vertices = vertices.ToArray();
        //mesh.triangles = triangles.ToArray();
        //mesh.RecalculateNormals();
        return success;
    }
    
    private static bool CreateObj(GameObject parent, OSMBuildingData osmBuildingData, Vector2[] way , string s)
    {
        GameObject child = new GameObject(s);
        GameObject.Instantiate(child, parent.transform);
        //TODO switch statement to create different objs and place on correct coordinates.
        //static class with all the CFGs as lists in there accessible without instantiation.
        return true;
    }
}