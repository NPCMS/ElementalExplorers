using System;
using System.Collections.Generic;
using UnityEngine;
using Object = UnityEngine.Object;

public static class DataToObjects
{
    public static bool TryCreateObjectOnWay(MeshFilter buildingMesh, OSMBuildingData building, string obj, ElevationData elevation)
    {
        Vector2[] way = building.footprint.ToArray();
        //way = MakeAntiClockwise(way);
        bool success = true;
        try
        {
            success = CreateObj(buildingMesh, building, way, obj, elevation);
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


    public static bool CreateDoor(MeshFilter buildingMesh, string s, ElevationData elevation)
    {
        float doorSize = 1.0f;
        
        // The layer to use for the  doors
        LayerMask doorLayer = LayerMask.GetMask("Default");
        
        // Get the vertices of the building mesh
        Vector3[] vertices = buildingMesh.sharedMesh.vertices;

        //TODO switch statement to create different objs and place on correct coordinates.
    
        var resource = Resources.Load("01_AssetStore/DoorPackFree/Prefab/DoorV6");
        GameObject doorPrefab = resource as GameObject;
        // Place doors on the mesh
        for (float i = 0; i < vertices.Length; i++)
        {
            // Check if the vertex is facing the right direction for a door
            if (Vector3.Dot(vertices[(int)i], Vector3.up) > 0.9f)
            {
                // TODO Check if there is enough space for a door
                // TODO heuristic for which wall to place on: either longest wall or wall closest to road.

                float nextIndex = i + 1;
                if (Vector3.Dot(vertices[(int)nextIndex], Vector3.up) > 0.9f)
                {

                    // Create a door at the midpoint between these two vertices vertex
                    Vector3 doorPos = buildingMesh.transform.TransformPoint((vertices[(int)i] + vertices[(int)nextIndex]) / 2f);
                    doorPos.y = (float)elevation.SampleHeightFromPosition(doorPos) + 0.1f;

                    // Compute the rotation of the door based on the angle of the wall at the midpoint between two vertices
                    Vector3 direction = vertices[(int)nextIndex] - vertices[(int)i];
                    Quaternion doorRotation = Quaternion.LookRotation(direction, Vector3.up);


                    GameObject door = Object.Instantiate(doorPrefab, doorPos, doorRotation);
                    door.transform.localScale = new Vector3(doorSize, doorSize, doorSize);
                    door.layer = doorLayer;
                    //break so there's only one door per building...
                    break;
                }

            }
        }
        
        return true;
    }


    public static bool CreateWindow(MeshFilter buildingMesh, string s, ElevationData elevation, int levelNum)
    {
        // The size of the windows to place on the mesh
        float windowSize = 0.5f;

        // The height at which to place the windows on the mesh
        float levelHeight = 3.0f;

        // The spacing between each window on the mesh
        float windowSpacing = 2.0f;

        int numWindows = 2;
        
        // The layers to use for the windows
        LayerMask windowLayer  = LayerMask.GetMask("Default");
        
        //GameObject child = new GameObject(s);
        // Get the vertices of the building mesh
        var sharedMesh = buildingMesh.sharedMesh;
        Vector3[] vertices = sharedMesh.vertices;
        Vector3[] normals = sharedMesh.normals;

        var resource = Resources.Load("Breakable Windows/Prefabs/Window");
        GameObject windowPrefab = resource as GameObject;
        // Place windows on the mesh
        for (float i = 0; i < vertices.Length; i++)
        {
            // Check if the vertex is facing the right direction for a door
            if (Vector3.Dot(vertices[(int)i], Vector3.up) > 0.9f)
            {
                // TODO Check if there is enough space for a window
                // TODO heuristic for which wall to place on: either longest wall or wall closest to road.
                if (Math.Abs(i + 1 - vertices.Length) < 1) break;
                float nextIndex = i + 1;
                if (Vector3.Dot(vertices[(int)nextIndex], Vector3.up) > 0.9f)
                {
                    
                    // Create a n windows at the between these two vertices 
                    Vector3 startPoint = buildingMesh.transform.TransformPoint(vertices[(int)i]);
                    for (int j = 0; j < numWindows; j++)
                    {
                        
                        
                        Vector3 windowPos = buildingMesh.transform.TransformPoint((vertices[(int)i] + vertices[(int)nextIndex]) / 2f);
                        windowPos.y = (float)elevation.SampleHeightFromPosition(windowPos) + 5f;

                        // Compute the rotation of the window based on the angle of the wall at the midpoint between two vertices
                        Vector3 direction = vertices[(int)nextIndex] - vertices[(int)i];
                        Quaternion windowRotation = Quaternion.LookRotation(direction, Vector3.up);

                        GameObject window = Object.Instantiate(windowPrefab, windowPos, windowRotation);
                        window.transform.Rotate(0,90,0);

                        //window.transform.localScale = new Vector3(windowSize, windowSize, windowSize);
                        window.layer = windowLayer;
                        
                        
                        
                        
                        
                        
                        
                        
                        
                        
                        // Vector3 direction = vertices[(int)nextIndex] - vertices[(int)i];
                        // // convert the vertex position to world space
                        // Vector3 windowPosition = buildingMesh.transform.TransformPoint(new Vector3(vertices[(int)i].x, vertices[(int)i].y, vertices[(int)i].z)); 
                        //
                        // //Set y coordinate
                        // windowPosition.y = (float)elevation.SampleHeightFromPosition(windowPosition) + 3f * levelNum;
                        // // get the window direction vector in world space
                        // Vector3 windowDirection = buildingMesh.transform.TransformDirection(direction); 
                        // // set the window position spaced equally along the building wall
                        // //windowPosition += windowDirection * windowSpacing * (i - numWindows/2); 
                        // // set the window rotation to face the correct direction
                        // Quaternion windowRotation = Quaternion.LookRotation(windowDirection, Vector3.up); 
                        // // flip the window around to face inside the building
                        // //windowRotation *= Quaternion.Euler(0f, 180f, 0f); 
                        // // create a new window at the selected position and rotation
                        // GameObject newWindow = Object.Instantiate(windowPrefab, windowPosition, windowRotation);
                        // newWindow.layer = windowLayer;





                        // Vector3 windowPos = buildingMesh.transform.TransformPoint((vertices[(int)i] + vertices[(int)nextIndex]) / 2f);
                        // windowPos.y = (float)elevation.SampleHeightFromPosition(windowPos) + 0.1f;
                        //
                        // // Compute the rotation of the window based on the angle of the wall at the midpoint between two vertices
                        // Vector3 direction = vertices[(int)nextIndex] - vertices[(int)i];
                        // Quaternion windowRotation = Quaternion.LookRotation(direction, Vector3.up);
                        //
                        //
                        // GameObject window = Object.Instantiate(windowPrefab, windowPos, windowRotation);
                        // window.transform.localScale = new Vector3(windowSize, windowSize, windowSize);
                        // window.layer = windowLayer;
                    }
                    //break so we only draw windows on one side...
                    //TODO in future draw windows on sides at right angle to this one I suppose.
                    //break;
                }

            }
        }
        return true;

    }

    
    private static bool CreateObj(MeshFilter buildingMesh, OSMBuildingData osmBuildingData, Vector2[] way , string s, ElevationData elevation)
    {
        // The size of the windows and doors to place on the mesh
        float windowSize = 0.5f;
        float doorSize = 1.0f;

        // The height at which to place the windows and doors on the mesh
        float windowHeight = 1.0f;

        // The spacing between each window and door on the mesh
        float windowSpacing = 2.0f;
        float doorSpacing = 4.0f;

        // The layers to use for the windows and doors
        LayerMask windowLayer  = LayerMask.GetMask("Default");
        LayerMask doorLayer = LayerMask.GetMask("Default");
        
        //GameObject child = new GameObject(s);
        // Get the vertices of the building mesh
        Vector3[] vertices = buildingMesh.sharedMesh.vertices;
        
        //GameObject.Instantiate(child, buildingMesh.transform);
        //TODO switch statement to create different objs and place on correct coordinates.
        if (s.Contains("door"))
        {
            var resource = Resources.Load("01_AssetStore/DoorPackFree/Prefab/DoorV6");
            GameObject doorPrefab = resource as GameObject;
            // Place doors on the mesh
            for (float i = 0; i < vertices.Length; i++)
            {
                // Check if the vertex is facing the right direction for a door
                if (Vector3.Dot(vertices[(int)i], Vector3.up) > 0.9f)
                {
                    // TODO Check if there is enough space for a door
                    // TODO heuristic for which wall to place on: either longest wall or wall closest to road.

                    float nextIndex = i + 1;
                    if (Vector3.Dot(vertices[(int)nextIndex], Vector3.up) > 0.9f)
                    {
                        
                        // Create a door at the midpoint between these two vertices vertex
                        Vector3 doorPos = buildingMesh.transform.TransformPoint((vertices[(int)i] + vertices[(int)nextIndex]) / 2f);
                        doorPos.y = (float)elevation.SampleHeightFromPosition(doorPos) + 0.1f;
                        
                        // Compute the rotation of the door based on the angle of the wall at the midpoint between two vertices
                        Vector3 direction = vertices[(int)nextIndex] - vertices[(int)i];
                        Quaternion doorRotation = Quaternion.LookRotation(direction, Vector3.up);
                        
                        
                        GameObject door = Object.Instantiate(doorPrefab, doorPos, doorRotation);
                        door.transform.localScale = new Vector3(doorSize, doorSize, doorSize);
                        door.layer = doorLayer;
                        //break so there's only one door per building...
                        break;
                    }

                }
            }
            
            
            
            
        }
        else if (s.Contains("window"))
        {
            GameObject windowPrefab = new GameObject();
            // Place windows on the mesh float because no more casting ;)
            for (float i = 0; i < vertices.Length; i++)
            {
                // Check if the vertex is facing the right direction for a window
                if (Vector3.Dot(vertices[(int)i], Vector3.up) > 0.9f)
                {
                    // Check if there is enough space for a window
                    if (i % windowSpacing == 0 && i + windowSpacing < vertices.Length)
                    {
                        // Create a window at the vertex
                        Vector3 windowPos = buildingMesh.transform.TransformPoint(vertices[(int)i]);
                        windowPos.y = windowHeight;
                        GameObject window = Object.Instantiate(windowPrefab, windowPos, Quaternion.identity);
                        window.transform.localScale = new Vector3(windowSize, windowSize, windowSize);
                        window.layer = windowLayer;
                    }
                }
            }
        }
        else if (s.Contains("roof"))
        {
            //Draw a roof
        }
        //static class with all the CFGs as lists in there accessible without instantiation.
        return true;
    }
}