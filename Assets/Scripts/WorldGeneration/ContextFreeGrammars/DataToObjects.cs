using System;
using System.Collections.Generic;
using System.Diagnostics.Tracing;
using System.Linq;
using Unity.VisualScripting;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;
using Object = UnityEngine.Object;
using Random = System.Random;

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
        float doorSize = 1.8f; // 6 foot tall doors

        // The layer to use for the  doors
        LayerMask doorLayer = LayerMask.GetMask("Default");

        // Get the vertices of the building mesh
        Vector3[] vertices = buildingMesh.sharedMesh.vertices;

        //TODO switch statement to create different objs and place on correct coordinates.

        Random rnd = new Random();
        int seed = rnd.Next(0, BuildingAssets.doorsPaths.Count);
        var resource = Resources.Load(BuildingAssets.doorsPaths[seed]);
        GameObject doorPrefab = resource as GameObject;
        // Place doors on the mesh
        for (float i = 0; i < vertices.Length; i++)
        {
            // Check if the vertex is facing the right direction for a door
            if (Vector3.Dot(vertices[(int)i], Vector3.up) > 0.9f)
            {
                // TODO Check if there is enough space for a door - always enough space...
                // TODO heuristic for which wall to place on: either longest wall or wall closest to road?

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
                    door.transform.parent = buildingMesh.transform;
                    //break so there's only one door per building...
                    break;
                }

            }
        }

        return true;
    }

    
    public static bool CreateWindow(MeshFilter buildingMesh, string s, ElevationData elevation, int levelNum, OSMBuildingData buildingData )
    {
        //TODO multiple windows per level
        bool isDoor = true;
        // The size of the windows to place on the mesh
        float windowSize = 4.0f;

        // The height at which to place the windows on the mesh
        float levelHeight = 3.0f;

        // The spacing between each window on the mesh
        float windowSpacing = 2.0f;
        

        // The layers to use for the windows
        LayerMask windowLayer = LayerMask.GetMask("Default");

        List<Vector3> prevPositions = new List<Vector3>(); 
        //GameObject child = new GameObject(s);
        // Get the vertices of the building mesh
        var sharedMesh = buildingMesh.sharedMesh;
        Vector3[] vertices = sharedMesh.vertices;
        Vector3[] normals = sharedMesh.normals;
        var position = buildingMesh.transform.position;
        float minHeight = getMinimumHeight(vertices);
        
        Random rnd = new Random();
        int seed = rnd.Next(0, BuildingAssets.windowsPaths.Count);
        var resource = Resources.Load(BuildingAssets.windowsPaths[seed]);
        GameObject windowPrefab = resource as GameObject;
        Debug.Log(windowPrefab);
        // Place windows on the mesh
        bool finished = false;

        for (int level = 1; level < buildingData.buildingLevels + 1; level++)
        {
            //initial offset for window calculation.
            //spacing per window calculation.
            //next window placement calculation.
            //if only space for one window center it.
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
                       
                        //if there is enough space for one window
                        float vertexDistance = Vector3.Distance(vertices[(int)i], vertices[(int)nextIndex]);
                        if (vertexDistance > windowSize + 0.4f)

                        {
                            var numWindows = vertexDistance / 4;
                            Debug.Log(numWindows);
                            if (numWindows < 2)
                            {
                                
                                Vector3 direction = vertices[(int)nextIndex] - vertices[(int)i];
                                Vector3 windowPos =
                                    buildingMesh.transform.TransformPoint(
                                        (vertices[(int)i] + vertices[(int)nextIndex]) / 2f);
                                windowPos.y = (float)elevation.SampleHeightFromPosition(windowPos) +
                                              ((windowSize + 0.4f) * level);
                                var midFirstCross = Vector3.Cross(windowPos, vertices[(int)i]);
                                var lastFirstCross = Vector3.Cross(vertices[(int)nextIndex], vertices[(int)i]);
                                //bool isOnMesh = Vector3.Dot(midFirstCross, lastFirstCross) < 0;
                                if (!isNearWindow(prevPositions, windowPos)) //&& isOnMesh)
                                {
                                    // Compute the rotation of the window based on the angle of the wall at the midpoint between two vertices
                                    
                                    Quaternion windowRotation = Quaternion.LookRotation(direction, Vector3.up);

                                    GameObject window = Object.Instantiate(windowPrefab, windowPos, windowRotation);
                                    window.transform.Rotate(0, 90, 0);
                                    //window.transform.localScale = new Vector3(0.5f, 0.5f, 0.5f);
                                    window.transform.position = window.transform.position + (0.08f * normals[(int)i]);
                                    window.transform.localScale = new Vector3(2, 2, 2);
                                    window.layer = windowLayer;
                                    window.transform.parent = buildingMesh.transform;
                                    prevPositions.Add(window.transform.position);
                                    //break;
                                }
                            }
                            Vector3 windowPosition =
                                buildingMesh.transform.TransformPoint(
                                    (vertices[(int)i] + vertices[(int)nextIndex]) / 2f);
                            var y = (float)elevation.SampleHeightFromPosition(windowPosition) +
                                    ((windowSize + 0.4f) * level);
                            var offset = Vector3.Distance(vertices[(int)i], vertices[(int)nextIndex])/ 2.5f;
                            for(int k = 1; k < numWindows + 1; k++)
                            {
                                Vector3 direction = (vertices[(int)nextIndex] - vertices[(int)i]).normalized;
                                Vector3 windowPos =
                                    buildingMesh.transform.TransformPoint(
                                        vertices[(int)i]) + (2f + 1f*k) * direction;
                                //(k * offset) * direction
                                windowPos.y = y;
                                var midFirstCross = Vector3.Cross(windowPos, vertices[(int)i]);
                                var lastFirstCross = Vector3.Cross(vertices[(int)nextIndex], vertices[(int)i]);
                                //bool isOnMesh = Vector3.Dot(midFirstCross, lastFirstCross) < 0;
                                if (!isNearWindow(prevPositions, windowPos)) //&& isOnMesh)
                                {
                                    // Compute the rotation of the window based on the angle of the wall at the midpoint between two vertices
                                    
                                    Quaternion windowRotation = Quaternion.LookRotation(direction, Vector3.up);

                                    GameObject window = Object.Instantiate(windowPrefab, windowPos, windowRotation);
                                    window.transform.Rotate(0, 90, 0);
                                    //window.transform.localScale = new Vector3(0.5f, 0.5f, 0.5f);
                                    window.transform.position = window.transform.position + (0.08f * normals[(int)i]);
                                    window.transform.localScale = new Vector3(2, 2, 2);
                                    window.layer = windowLayer;
                                    window.transform.parent = buildingMesh.transform;
                                    prevPositions.Add(window.transform.position);
                                    //break;
                                }

                               
                                // anotherWindow = Vector3.Distance(vertices[(int)i], new Vector3(windowPos.x, vertices[(int)i].y, windowPos.z)) > windowSize + 0.4f && 
                                //                 Vector3.Distance(new Vector3(windowPos.x, vertices[(int)i].y, windowPos.z), vertices[(int)nextIndex]) < vertexDistance;
                                // Debug.Log(vertices[(int)i].y);
                                // Debug.Log(anotherWindow);
                            }
                        }
                    }
                }
            }
            if (finished)
            {
                break;
            }
        }
        return true;
    }


private static bool isNearWindow(List<Vector3> prevPositions, Vector3 windowPos)
    {
        bool nearby = false;
        foreach (Vector3 vector in prevPositions)
        {
            if (Vector3.Distance(vector, windowPos) < 1f)
            {
                nearby = true;
                break;
            }
        }
        return nearby;
    }


private static float getMinimumHeight(Vector3[] vertices)
{
    float minHeight = vertices[0].y;
    foreach (Vector3 vertex in vertices)
    {
        if (vertex.y < minHeight)
        {
            minHeight = vertex.y;
        }
    }
    return minHeight;
}
    
    public static bool CreateRoof(GameObject building, string s, ElevationData elevation, OSMBuildingData buildingData)
    {
        Vector2[] footprint = MakeAntiClockwise(buildingData.footprint.ToArray());

        Bounds bounds = building.GetComponent<MeshFilter>().sharedMesh.bounds;
        
        float buildingHeight = buildingData.buildingHeight;
        
        var data = building.GetComponent<MeshFilter>().sharedMesh;
        
        //TODO acquire corners.
        if (!((footprint.Length > 3) && (footprint.Length < 10)))
        {
            return true;
        }
        
        //get maximum x
        Vector2 maxX = getMaxXValue(footprint);
        //get minimum x
        Vector2 minX = getMinXValue(footprint);
        //get maximum z
        Vector2 maxZ = getMaxZValue(footprint);
        //get minimum z
        Vector2 minZ = getMinZValue(footprint);
        
        
        // order vertices to form correct bounding box (basically convex hull problem, using heuristic method)
        // generates ABCD rectangle
        // WARNING: MAY BREAK ON PERFECT SQUARE FOOTPRINT
        // create list
        List<Vector2> vertsToBeOrdered = new List<Vector2>() { maxX, maxZ, minZ };

        // select A as first vert
        Vector2 A = minX;
        // B is the shortest distance from A
        Vector2 B = GetClosestPointToVert(A, vertsToBeOrdered);
        // remove selected vert
        vertsToBeOrdered.Remove(B);
        // C is closest point to C of remaining bounds
        Vector2 C = GetClosestPointToVert(B, vertsToBeOrdered);
        vertsToBeOrdered.Remove(C);
        // finally D is closest point to C (final remaining point)
        Vector2 D = vertsToBeOrdered.First();
        
        // We now have ABCD tri and can generate mesh
        
        
        Vector2 ABMiddle = Vector2.Lerp(A, B, 0.5f);
        Vector2 CDMiddle = Vector2.Lerp(C, D, 0.5f);
        
        // generate vertex positions using ABCD rect
        // v4 = A
        Vector3 v4 = new Vector3(A.x, buildingHeight, A.y);
        // v2 = B
        Vector3 v2 = new Vector3(B.x, buildingHeight, B.y);
        // v3 = C
        Vector3 v3 = new Vector3(C.x, buildingHeight, C.y);
        // v5 = D
        Vector3 v5 = new Vector3(D.x, buildingHeight, D.y);
        // v0 = mid(A,B)
        Vector3 v0 = new Vector3(ABMiddle.x, buildingHeight + (buildingHeight / 6), ABMiddle.y);
        // v1 = mid(C, D0
        Vector3 v1 = new Vector3(CDMiddle.x, buildingHeight + (buildingHeight / 6), CDMiddle.y);
        
        
        
        
        // 6 points of triangular prism
        Vector3[] oldVertices = new Vector3[]
        {
            v0,v1,v2,v3,v4,v5
        };

        oldVertices = MakeAntiClockwise(oldVertices);
        
        // 8 tris, 5 faces
        int[] triangles = new int[]
        {
            1,2,0,
            0,2,1,
            3,4,2,
            2,4,3,
            3,1,5,
            5,1,3,
            5,0,4,
            4,0,5,
            0,2,4,
            4,2,0,
            1,3,2,
            2,3,1,
            3,5,4,
            4,5,3,
            5,1,0,
            0,1,5
        };
        
        // Duplicate vertices to allow for flat shading
        // https://answers.unity.com/questions/798510/flat-shading.html
        Vector3[] vertices = new Vector3[triangles.Length];
        for (int i = 0; i < triangles.Length; i++) {
            vertices[i] = oldVertices[triangles[i]];
            triangles[i] = i;
        }
        
        
        Mesh mesh = new Mesh
        {
            vertices = vertices,
            triangles = triangles,
        };
        mesh.RecalculateBounds();
        mesh.RecalculateNormals();
        mesh.uv = Unwrapping.GeneratePerTriangleUV(mesh);

        // Create a new game object with a mesh renderer and filter

        Bounds roofBounds = mesh.bounds;

        if (!(bounds.size.x * bounds.size.z > roofBounds.size.x * roofBounds.size.z * 4))
        {
            GameObject prism = new GameObject("Triangular Prism");
            Random rnd = new Random();
            int seed = rnd.Next(0, BuildingAssets.materialsPaths.Count);
            prism.AddComponent<MeshRenderer>().material = Resources.Load<Material>(BuildingAssets.materialsPaths[seed]);
            MeshFilter meshFilter = prism.AddComponent<MeshFilter>();
            meshFilter.mesh = mesh;
        
            Vector3 buildingSize = building.GetComponent<MeshFilter>().sharedMesh.bounds.size;
            var position = building.transform.position;
            prism.transform.position = new Vector3(position.x, position.y, position.z);
            prism.transform.rotation = Quaternion.identity;
            prism.transform.parent = building.transform;

        }
        
        
        
        
        return true;
    }


    private static Vector2 GetClosestPointToVert(Vector2 target, List<Vector2> options)
    {
        float minDist = 100000;
        Vector2 currentClosest = new Vector2(0, 0);
        foreach (Vector2 option in options)
        {
            if (Vector2.Distance(target, option) < minDist)
            {
                minDist = Vector2.Distance(target, option);
                currentClosest = option;
            }
        }
        return currentClosest;
    }
    
    private static Vector2 getMinXValue(Vector2[] vertices)
    {
        Vector2 minX = vertices[0];
        foreach (Vector2 vertex in vertices)
        {
            if (vertex.x < minX.x)
            {
                minX = vertex;
            }
        }

        return minX;
    }
    
    private static Vector2 getMaxXValue(Vector2[] vertices)
    {
        Vector2 maxX = vertices[0];
        foreach (Vector2 vertex in vertices)
        {
            if (vertex.x > maxX.x)
            {
                maxX = vertex;
            }
        }

        return maxX;
    }
    
    private static Vector2 getMaxZValue(Vector2[] vertices)
    {
        Vector2 maxZ = vertices[0];
        foreach (Vector2 vertex in vertices)
        {
            if (vertex.y > maxZ.y)
            {
                maxZ = vertex;
            }
        }
        return maxZ;
    }
    private static Vector2 getMinZValue(Vector2[] vertices)
    {
        Vector2 minZ = vertices[0];
        foreach (Vector2 vertex in vertices)
        {
            if (vertex.y < minZ.y)
            {
                minZ = vertex;
            }
        }
        return minZ;
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



    private static Vector3[] MakeAntiClockwise(Vector3[] way)
    {
        
        float sum = 0;
        for (int i = 0; i < way.Length; i++)
        {
            Vector3 v0 = way[i];
            Vector3 v1 = way[ReMap(i + 1, way.Length)];
            sum += (v1.x - v0.x) * (v1.z + v0.z);
        }

        if (sum > 0)
        {
            var l = new List<Vector3>(way);
            l.Reverse();
            return l.ToArray();
        }

        return way;
        
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