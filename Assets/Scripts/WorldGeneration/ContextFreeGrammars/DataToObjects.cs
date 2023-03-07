using System;
using System.Collections.Generic;
using System.Diagnostics.Tracing;
using Unity.VisualScripting;
using UnityEditor;
using UnityEngine;
using Object = UnityEngine.Object;

public static class DataToObjects
{
    public static bool TryCreateObjectOnWay(MeshFilter buildingMesh, OSMBuildingData building, string obj,
        ElevationData elevation)
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
        float doorSize = 2.0f;

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
                    Vector3 doorPos =
                        buildingMesh.transform.TransformPoint((vertices[(int)i] + vertices[(int)nextIndex]) / 2f);
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

    //current prefab is 5f wide so this is hard coded
    public static bool CreateWindow(MeshFilter buildingMesh, string s, ElevationData elevation, int levelNum)
    {
        //TODO multiple levels of windows. make sure each window fits on mesh! Scale each window to building size

        // The size of the windows to place on the mesh
        float windowSize = 5f;

        // The height at which to place the windows on the mesh
        float levelHeight = 3.0f;

        // The spacing between each window on the mesh
        float windowSpacing = 2.0f;

        int numWindows = 2;

        // The layers to use for the windows
        LayerMask windowLayer = LayerMask.GetMask("Default");

        //GameObject child = new GameObject(s);
        // Get the vertices of the building mesh
        var sharedMesh = buildingMesh.sharedMesh;
        Vector3[] vertices = sharedMesh.vertices;
        Vector3[] normals = sharedMesh.normals;
        var position = buildingMesh.transform.position;
        var levels = (elevation.SampleHeightFromPosition(position) - position.y) / 3;

        var resource = Resources.Load("DetatchedHousePrefabs/Windows/Var1/Prefabs/Window2");
        GameObject windowPrefab = resource as GameObject;
        Debug.Log(windowPrefab);
        // Place windows on the mesh
        for (int level = 1; level < levels + 1; level++)
        {
            for (float i = 0; i < vertices.Length; i++)
            {
                // Check if the vertex is facing the right direction for a door
                if (Vector3.Dot(vertices[(int)i], Vector3.up) > 0.9f)
                {
                    // TODO Check if there is enough space for a window
                    // TODO heuristic for which wall to place on: either longest wall or wall closest to road.
                    if (System.Math.Abs(i + 1 - vertices.Length) < 1) break;
                    float nextIndex = i + 1;
                    if (Vector3.Dot(vertices[(int)nextIndex], Vector3.up) > 0.9f)
                    {
                        if (Vector3.Distance(vertices[(int)i], vertices[(int)nextIndex]) > windowSize + 0.4f)
                        {

                            // Create a n windows at the between these two vertices 
                            Vector3 startPoint = buildingMesh.transform.TransformPoint(vertices[(int)i]);
                            for (int j = 0; j < numWindows; j++)
                            {


                                Vector3 windowPos =
                                    buildingMesh.transform.TransformPoint(
                                        (vertices[(int)i] + vertices[(int)nextIndex]) / 2f);
                                windowPos.y = (float)elevation.SampleHeightFromPosition(windowPos) +
                                              ((windowSize + 0.4f) * level);

                                // Compute the rotation of the window based on the angle of the wall at the midpoint between two vertices
                                Vector3 direction = vertices[(int)nextIndex] - vertices[(int)i];
                                Quaternion windowRotation = Quaternion.LookRotation(direction, Vector3.up);

                                GameObject window = Object.Instantiate(windowPrefab, windowPos, windowRotation);
                                window.transform.Rotate(-90, 90, 0);
                                window.transform.localScale = new Vector3(0.5f, 0.5f, 0.5f);
                                window.transform.position = window.transform.position + (0.01f * normals[(int)i]);
                                //window.transform.localScale = new Vector3(windowSize, windowSize, windowSize);
                                window.layer = windowLayer;
                            }
                            //break so we only draw windows on one side...
                            //TODO in future draw windows on sides at right angle to this one I suppose.
                            //break;
                        }
                    }

                }
            }
        }

        return true;

    }

    public static bool CreateRoof(GameObject building, string s, ElevationData elevation, OSMBuildingData buildingData)
    {
        Vector2[] footprint = MakeAntiClockwise(buildingData.footprint.ToArray());

        //Bounds bounds = building.GetComponent<MeshFilter>().sharedMesh.bounds;
        
        float buildingHeight = buildingData.buildingHeight;
        
        var data = building.GetComponent<MeshFilter>().sharedMesh;
        
        //TODO acquire corners.
        
        Vector2 startMiddle = (footprint[0] + footprint[1]) / 2;
        Vector2 endMiddle = (footprint[^1] + footprint[^2]) / 2;
        
        Vector3 v0 = new Vector3(footprint[0].x, buildingHeight, footprint[0].y );
        Vector3 v1 = new Vector3(startMiddle.x, buildingHeight, startMiddle.y);
        Vector3 v2 = new Vector3(footprint[1].x, buildingHeight, footprint[1].y );
        v1.y += buildingData.buildingHeight / 3;
        
        Vector3 v3 = new Vector3(footprint[^1].x, buildingHeight, footprint[^1].y );
        Vector3 v4 = new Vector3(endMiddle.x, buildingHeight, endMiddle.y);
        Vector3 v5 = new Vector3(footprint[^2].x, buildingHeight, footprint[^2].y );
        v4.y += buildingData.buildingHeight / 3;

        Vector3[] points = new Vector3[]
        {
            v0,v1,v2,v3,v4,v5
        };

        // Calculate the vertices, normals, UVs, and triangles
        Vector3[] vertices = new Vector3[]
        {
            // Top triangle vertices
            points[0], points[1], points[2],
            points[0], points[2], points[1],

            // Bottom triangle vertices
            points[3], points[5], points[4],
            points[3], points[4], points[5],

            // Side vertices
            points[0], points[3], points[4],
            points[0], points[4], points[1],
            points[1], points[4], points[5],
            points[1], points[5], points[2],
            points[2], points[5], points[3],
            points[2], points[3], points[0],
        };

        Vector3[] normals = new Vector3[]
        {
            // Top triangle normals
            Vector3.Cross(points[1] - points[0], points[2] - points[0]).normalized,
            Vector3.Cross(points[1] - points[0], points[2] - points[0]).normalized,

            // Bottom triangle normals
            Vector3.Cross(points[5] - points[3], points[4] - points[3]).normalized,
            Vector3.Cross(points[5] - points[3], points[4] - points[3]).normalized,

            // Side normals
            Vector3.Cross(points[3] - points[0], points[4] - points[0]).normalized,
            Vector3.Cross(points[4] - points[1], points[5] - points[1]).normalized,
            Vector3.Cross(points[5] - points[2], points[3] - points[2]).normalized,
            Vector3.Cross(points[4] - points[0], points[1] - points[0]).normalized,
            Vector3.Cross(points[5] - points[1], points[2] - points[1]).normalized,
            Vector3.Cross(points[3] - points[2], points[0] - points[2]).normalized,
        };
        
        // Calculate the UVs for the mesh
        Vector2[] uvs = new Vector2[]
        {
            // Top triangle UVs
            new Vector2(0, 1),
            new Vector2(Vector2.Distance(points[0], points[1]), 1),
            new Vector2(Vector2.Distance(points[0], points[1]) + Vector2.Distance(points[2], points[3]), 0),

            // Bottom triangle UVs
            new Vector2(0, 0),
            new Vector2(Vector2.Distance(points[3], points[4]), 0),
            new Vector2(Vector2.Distance(points[3], points[4]) + Vector2.Distance(points[1], points[0]), 1),

            // Side UVs
            new Vector2(0, 0),
            new Vector2(Vector2.Distance(points[2], points[3]), 0),
            new Vector2(Vector2.Distance(points[2], points[3]), Vector2.Distance(points[2], points[1])),
            new Vector2(0, Vector2.Distance(points[2], points[1])),
            new Vector2(Vector2.Distance(points[0], points[1]), Vector2.Distance(points[0], points[3])),
            new Vector2(Vector2.Distance(points[0], points[2]), Vector2.Distance(points[0], points[1])),
            new Vector2(Vector2.Distance(points[0], points[2]), Vector2.Distance(points[0], points[5])),
            new Vector2(Vector2.Distance(points[0], points[5]), Vector2.Distance(points[0], points[1])),
            new Vector2(Vector2.Distance(points[2], points[4]), Vector2.Distance(points[2], points[3])),
            new Vector2(Vector2.Distance(points[4], points[5]), Vector2.Distance(points[4], points[3])),
            new Vector2(Vector2.Distance(points[4], points[5]), Vector2.Distance(points[4], points[2])),
            new Vector2(Vector2.Distance(points[0], points[1]), Vector2.Distance(points[0], points[3])),
            new Vector2(Vector2.Distance(points[0], points[2]), Vector2.Distance(points[0], points[1])),
            new Vector2(Vector2.Distance(points[2], points[3]), Vector2.Distance(points[2], points[1])),
            new Vector2(Vector2.Distance(points[4], points[3]), Vector2.Distance(points[4], points[1])),
            new Vector2(Vector2.Distance(points[2], points[4]), Vector2.Distance(points[2], points[1])),
            new Vector2(Vector2.Distance(points[0], points[3]), Vector2.Distance(points[0], points[5])),
            new Vector2(Vector2.Distance(points[2], points[3]), Vector2.Distance(points[2], points[5])),
            new Vector2(Vector2.Distance(points[4], points[5]), Vector2.Distance(points[4], points[3])),
            new Vector2(Vector2.Distance(points[0], points[5]), Vector2.Distance(points[0], points[1])), 
        };
        
        int[] triangles = new int[]
        {
            //Top triangles
            0, 1, 2,
            //2, 1, 0,
            3, 4, 5,
            //5, 4, 3,

            //Bottom triangles
            6, 7, 8,
            //8, 7, 6,
            9, 10, 11,
            //11, 10, 9,

            //Side triangles
            12, 13, 14,
            //14, 13, 12,
            15, 16, 17,
            //17, 16, 15,
            18, 19, 20,
            //20, 19, 18,
            21, 22, 23,
            //23, 22, 21,
            24, 25, 26,
            //26, 25, 24,
            27, 28, 29,
            //29, 28, 27,

            // // Additional ones?
            // 7, 10, 19,
            // 19, 10, 7,
            // 7, 19, 18,
            // 18, 19, 7,
            // 6, 20, 13,
            // 13, 20, 6,
            // 6, 13, 12,
            // 12, 13, 6,
            // 9, 16, 22,
            // 22, 16, 9,
            // 9, 22, 25,
            // 25, 22, 9,
            // 8, 28, 15,
            // 15, 28, 8,
            // 8, 15, 21,
            // 21, 15, 8,
            //
            // //Fix winding order?
            // 14, 13, 10,
            // 10, 13, 14,
            // 13, 14, 10,
            // 10, 14, 13,
            // 19, 16, 7,
            // 7, 16, 19,
            // 13, 16, 19,
            // 19, 16, 13
        };


        
        Mesh mesh = new Mesh
        {
            vertices = vertices,
            normals = normals,
            uv = uvs,
            triangles = triangles
        };
        mesh.RecalculateNormals();
        Unwrapping.GeneratePerTriangleUV(mesh);
        

        // Create a new game object with a mesh renderer and filter
        GameObject prism = new GameObject("Triangular Prism");
        MeshRenderer meshRenderer = prism.AddComponent<MeshRenderer>();
        //meshRenderer.material = material;
        MeshFilter meshFilter = prism.AddComponent<MeshFilter>();
        meshFilter.mesh = mesh;
        
        Vector3 buildingSize = building.GetComponent<MeshFilter>().sharedMesh.bounds.size;
        var position = building.transform.position;
        prism.transform.position = new Vector3(position.x, position.y, position.z);
        prism.transform.rotation = Quaternion.identity;
        
        
        
        return true;
    }



    private static Vector3[] CalculateNormals(Vector3[] vertices, int[] triangles)
    {
        Vector3[] normals = new Vector3[vertices.Length];

        // Calculate the normal of each face
        for (int i = 0; i < triangles.Length; i += 3)
        {
            int index0 = triangles[i];
            int index1 = triangles[i + 1];
            int index2 = triangles[i + 2];

            Vector3 side1 = vertices[index1] - vertices[index0];
            Vector3 side2 = vertices[index2] - vertices[index0];
            Vector3 normal = Vector3.Cross(side1, side2).normalized;

            normals[index0] += normal;
            normals[index1] += normal;
            normals[index2] += normal;
        }

        // Normalise the normals
        for (int i = 0; i < normals.Length; i++)
        {
            normals[i] = normals[i].normalized;
        }

        return normals;
    }
    
    private static Vector2[] MakeAntiClockwise(Vector2[] way)
    {
        float sum = 0;
        for (int i = 0; i < way.Length; i++)
        {
            Vector2 v0 = way[i];
            Vector2 v1 = way[ReMap(i + 1, way.Length)];
            sum += (v1.x - v0.x) * (v1.y + v0.y);
        }

        if (sum > 0)
        {
            var l = new List<Vector2>(way);
            l.Reverse();
            return l.ToArray();
        }

        return way;
    }
    
    private static int ReMap(int i, int length)
    {
        return i < 0 ? length + i : (i >= length ? i - length : i);
    }

    private static Vector2[] MakeClockwise(Vector2[] way)
    {
        float sum = 0;
        for (int i = 0; i < way.Length; i++)
        {
            Vector2 v0 = way[i];
            Vector2 v1 = way[ReMap(i + 1, way.Length)];
            sum += (v1.x - v0.x) * (v1.y + v0.y);
        }

        if (sum < 0)
        {
            var l = new List<Vector2>(way);
            l.Reverse();
            return l.ToArray();
        }

        return way;
    }
    
    
    private static int[] CombineTriangles(params int[][] arrays)
    {
        int totalLength = 0;
        foreach (int[] array in arrays)
        {
            totalLength += array.Length;
        }

        int[] combinedArray = new int[totalLength];

        int currentIndex = 0;
        foreach (int[] array in arrays)
        {
            array.CopyTo(combinedArray, currentIndex);
            currentIndex += array.Length;
        }

        return combinedArray;
    }
    // Combine vertices
    private static Vector3[] CombineVertices(params Vector3[][] arrays)
    {
        int totalLength = 0;
        foreach (Vector3[] array in arrays)
        {
            totalLength += array.Length;
        }

        Vector3[] combinedArray = new Vector3[totalLength];

        int currentIndex = 0;
        foreach (Vector3[] array in arrays)
        {
            array.CopyTo(combinedArray, currentIndex);
            currentIndex += array.Length;
        }

        return combinedArray;
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
