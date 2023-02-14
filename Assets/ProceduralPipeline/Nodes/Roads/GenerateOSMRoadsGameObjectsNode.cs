using System;
using System.Collections;
using System.Collections.Generic;
using UnityEditor.EventSystems;
using UnityEditor.Experimental.GraphView;
using UnityEngine;
using PathCreation;
using XNode;

[CreateNodeMenu("Roads/Generate OSM Road GameObjects")]
public class GenerateOSMRoadsGameObjectsNode : ExtendedNode
{

    [Input] public OSMRoadsData[] roadsData;
    [Input] public Material material;
    [Output] public GameObject[] roadsGameObjects;

    // Return the correct value of an output port when requested
    public override object GetValue(NodePort port)
    {

        if (port.fieldName == "roadsGameObjects")
        {
            return roadsGameObjects;
        }
        return null;
    }

    public override void CalculateOutputs(Action<bool> callback)
    {
        // setup inputs
        OSMRoadsData[] roads = GetInputValue("roadsData", roadsData);

        // setup outputs
        List<GameObject> gameObjects = new List<GameObject>();

        // create parent game object
        GameObject roadsParent = new GameObject("Roads");

        Material mat = GetInputValue("material", material);
        // iterate through road classes
        foreach (OSMRoadsData road in roads)
        {
            
            GameObject roadGO = CreateGameObjectFromRoadData(road, roadsParent.transform, mat);
            gameObjects.Add(roadGO);
        }
        roadsGameObjects = gameObjects.ToArray();
        callback.Invoke(true);
    }

    private Mesh CreateRoadMesh (VertexPath path) {
            float roadWidth = 4f;
            float thickness = 1f;
            bool flattenSurface = true;
            Vector3[] verts = new Vector3[path.NumPoints * 8];
            Vector2[] uvs = new Vector2[verts.Length];
            Vector3[] normals = new Vector3[verts.Length];

            int numTris = 2 * (path.NumPoints - 1) + ((path.isClosedLoop) ? 2 : 0);
            int[] roadTriangles = new int[numTris * 3];
            int[] underRoadTriangles = new int[numTris * 3];
            int[] sideOfRoadTriangles = new int[numTris * 2 * 3];

            int vertIndex = 0;
            int triIndex = 0;

            // Vertices for the top of the road are layed out:
            // 0  1
            // 8  9
            // and so on... So the triangle map 0,8,1 for example, defines a triangle from top left to bottom left to bottom right.
            int[] triangleMap = { 0, 8, 1, 1, 8, 9 };
            int[] sidesTriangleMap = { 4, 6, 14, 12, 4, 14, 5, 15, 7, 13, 15, 5 };

            bool usePathNormals = !(path.space == PathSpace.xyz && flattenSurface);

            for (int i = 0; i < path.NumPoints; i++) {
                Vector3 localUp = (usePathNormals) ? Vector3.Cross (path.GetTangent (i), path.GetNormal (i)) : path.up;
                Vector3 localRight = (usePathNormals) ? path.GetNormal (i) : Vector3.Cross (localUp, path.GetTangent (i));

                // Find position to left and right of current path vertex
                Vector3 vertSideA = path.GetPoint (i) - localRight * Mathf.Abs (roadWidth);
                Vector3 vertSideB = path.GetPoint (i) + localRight * Mathf.Abs (roadWidth);

                // Add top of road vertices
                verts[vertIndex + 0] = vertSideA;
                verts[vertIndex + 1] = vertSideB;
                // Add bottom of road vertices
                verts[vertIndex + 2] = vertSideA - localUp * thickness;
                verts[vertIndex + 3] = vertSideB - localUp * thickness;

                // Duplicate vertices to get flat shading for sides of road
                verts[vertIndex + 4] = verts[vertIndex + 0];
                verts[vertIndex + 5] = verts[vertIndex + 1];
                verts[vertIndex + 6] = verts[vertIndex + 2];
                verts[vertIndex + 7] = verts[vertIndex + 3];

                // Set uv on y axis to path time (0 at start of path, up to 1 at end of path)
                uvs[vertIndex + 0] = new Vector2 (0, path.times[i]);
                uvs[vertIndex + 1] = new Vector2 (1, path.times[i]);

                // Top of road normals
                normals[vertIndex + 0] = localUp;
                normals[vertIndex + 1] = localUp;
                // Bottom of road normals
                normals[vertIndex + 2] = -localUp;
                normals[vertIndex + 3] = -localUp;
                // Sides of road normals
                normals[vertIndex + 4] = -localRight;
                normals[vertIndex + 5] = localRight;
                normals[vertIndex + 6] = -localRight;
                normals[vertIndex + 7] = localRight;

                // Set triangle indices
                if (i < path.NumPoints - 1 || path.isClosedLoop) {
                    for (int j = 0; j < triangleMap.Length; j++) {
                        roadTriangles[triIndex + j] = (vertIndex + triangleMap[j]) % verts.Length;
                        // reverse triangle map for under road so that triangles wind the other way and are visible from underneath
                        underRoadTriangles[triIndex + j] = (vertIndex + triangleMap[triangleMap.Length - 1 - j] + 2) % verts.Length;
                    }
                    for (int j = 0; j < sidesTriangleMap.Length; j++) {
                        sideOfRoadTriangles[triIndex * 2 + j] = (vertIndex + sidesTriangleMap[j]) % verts.Length;
                    }

                }

                vertIndex += 8;
                triIndex += 6;
            }

            Mesh mesh = new Mesh();
            mesh.vertices = verts;
            mesh.uv = uvs;
            mesh.normals = normals;
            mesh.subMeshCount = 3;
            mesh.SetTriangles (roadTriangles, 0);
            mesh.SetTriangles (underRoadTriangles, 1);
            mesh.SetTriangles (sideOfRoadTriangles, 2);
            mesh.RecalculateBounds ();
            return mesh;
        }


    private GameObject CreateGameObjectFromRoadData(OSMRoadsData roadData, Transform parent, Material mat)
    {
        Vector2[] vertices = roadData.footprint.ToArray();
        Vector3[] vertices3D = new  Vector3[vertices.Length];
        for(int j = 0; j< vertices.Length; j++)
        {
            vertices3D[j] = new Vector3(vertices[j].x, roadData.elevation, vertices[j].y);
        } 
        VertexPath vertexPath = null;
        // create new game object
        GameObject temp = new GameObject(roadData.name);
        temp.transform.parent = parent;
        temp.transform.Rotate(new Vector3(90, 0, 0));
        if (vertices3D.Length > 1)
        {   
            vertexPath = RoadCreator.GeneratePath(vertices3D, false, temp);

        }
        

        //sUnity.Instantiate(temp, parent, position, rotation);
        //AddNodes(roadData, temp);

        
        if (vertexPath != null)
        {
            Mesh mesh = CreateRoadMesh(vertexPath);
            MeshFilter meshFilter = temp.AddComponent<MeshFilter>();        
            meshFilter.sharedMesh = mesh;   
            temp.AddComponent<MeshCollider>().sharedMesh = mesh;
            temp.AddComponent<MeshRenderer>().sharedMaterial = mat;
            temp.transform.position = new Vector3(roadData.center.x, roadData.elevation, roadData.center.y);
            //meshFilter.mesh = mesh;
        }
        else{
            Debug.Log("Failure to generate a gameObject road");
        }
        
        
        // triangulate mesh
        //bool success = WayToMesh.TryCreateRoad(roadData, out Mesh roadMesh);
        bool success = true;
        temp.name = success ? roadData.name : "Failed Road";
        // set mesh filter
        // add collider and renderer
        
        // apply transform updates
        
        return temp;
    }
}