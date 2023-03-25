using System;
using System.Collections;
using System.Collections.Generic;
using PathCreation;
using UnityEngine;
using XNode;
using Random = System.Random;

[CreateNodeMenu("Roads/Roads Data to Paths")]
public class ConvertRoadsIntoPathsNode : AsyncExtendedNode
{

    [Input] public List<OSMRoadsData> roads;
    [Input] public ElevationData elevationData;
    [Input] public Shader roadShader;
    [Input] public bool debug;
    [Output] public List<GameObjectData> gameObjectsData;

    public override object GetValue(NodePort port)
    {
        if (port.fieldName == "gameObjectsData") return gameObjectsData;
        return null;
    }

    protected override void CalculateOutputsAsync(Action<bool> callback)
    {
        List<OSMRoadsData> roadList = GetInputValue("roads", roads);
        ElevationData elevation = GetInputValue("elevationData", elevationData);
        Shader shader = GetInputValue("roadShader", roadShader);
        Random random = new Random(0);

        gameObjectsData = new List<GameObjectData>();
        
        foreach (OSMRoadsData road in roadList)
        {
            var roadDeltaHeight = random.NextDouble() / 100;
            GameObjectData go = CreateGameObjectFromRoadData(road, elevation, (float)roadDeltaHeight, shader);
            if (go != null)
            {
                gameObjectsData.Add(go);
            }
        }
    }

    private GameObjectData CreateGameObjectFromRoadData(OSMRoadsData roadData, ElevationData elevation, float deltaHeight, Shader shader)
    {
        Vector2[] vertices = roadData.footprint.ToArray();
        Vector3[] vertices3D = new Vector3[vertices.Length];
        float roadLength = 0f;
        for (int j = 0; j < vertices.Length; j++)
        {
            vertices3D[j] = new Vector3(vertices[j].x, 0.5f, vertices[j].y);
            if (j != vertices.Length - 1)
                roadLength += Vector2.Distance(vertices[j], vertices[j + 1]);
        }
        VertexPath vertexPath;
        // create new game object
        if (vertices3D.Length > 1)
        {
            vertexPath = RoadCreator.GeneratePath(vertices3D, false, temp);
        }
        else
        {
            if (debug) Debug.LogWarning("Road with 0 or 1 vertices found. Skipping: " + roadData.footprint.Count);
            return null;
        }

        if (vertexPath != null)
        {
            SerializableMeshInfo mesh = CreateRoadMesh(vertexPath, elevationData, deltaHeight);
            return new RoadGameObjectData(new Vector3(roadData.center.x, 0, roadData.center.y), Vector3.zero, Vector3.one, mesh, shader, roadLength);
        }

        Debug.LogError("Way shouldn't have a null vertex path. This should have been caught");
        return null;
    }

    // credit to Sebastian Lague
    private static SerializableMeshInfo CreateRoadMesh(VertexPath path, ElevationData elevation, float deltaHeight)
    {
        const float roadWidth = 4f;
        const float thickness = 0f;
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
        int[] triangleMap = {0, 8, 1, 1, 8, 9};
        int[] sidesTriangleMap = {4, 6, 14, 12, 4, 14, 5, 15, 7, 13, 15, 5};

    
        // bool usePathNormals = !(path.space == PathSpace.xyz && flattenSurface);
        bool usePathNormals = false;

        for (int i = 0; i < path.NumPoints; i++)
        {
            Vector3 localUp = (usePathNormals) ? Vector3.Cross(path.GetTangent(i), path.GetNormal(i)) : path.up;
            Vector3 localRight = (usePathNormals) ? path.GetNormal(i) : Vector3.Cross(localUp, path.GetTangent(i));

            // Find position to left and right of current path vertex
            Vector3 vertSideA = path.GetPoint(i) - localRight * Mathf.Abs(roadWidth);
            Vector3 vertSideB = path.GetPoint(i) + localRight * Mathf.Abs(roadWidth);

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
            uvs[vertIndex + 0] = new Vector2(0, path.times[i]);
            uvs[vertIndex + 1] = new Vector2(1, path.times[i]);

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
            if (i < path.NumPoints - 1 || path.isClosedLoop)
            {
                for (int j = 0; j < triangleMap.Length; j++)
                {
                    roadTriangles[triIndex + j] = (vertIndex + triangleMap[j]) % verts.Length;
                    // reverse triangle map for under road so that triangles wind the other way and are visible from underneath
                    underRoadTriangles[triIndex + j] =
                        (vertIndex + triangleMap[triangleMap.Length - 1 - j] + 2) % verts.Length;
                }

                for (int j = 0; j < sidesTriangleMap.Length; j++)
                {
                    sideOfRoadTriangles[triIndex * 2 + j] = (vertIndex + sidesTriangleMap[j]) % verts.Length;
                }
            }

            vertIndex += 8;
            triIndex += 6;
        }
        
        // todo convert to world space and snap to correct height

        float[] flattenedVerts = new float[verts.Length * 3];
        for (int i = 0; i < verts.Length; i++)
        {
            flattenedVerts[3 * i] = verts[i].x;
            flattenedVerts[3 * i + 1] = verts[i].y;
            flattenedVerts[3 * i + 2] = verts[i].z;
        }

        float[] flattenedUVs = new float[uvs.Length * 2];
        for (int i = 0; i < uvs.Length; i++)
        {
            flattenedUVs[2 * i] = uvs[i].x;
            flattenedUVs[2 * i + 1] = uvs[i].y;
        }

        float[] flattenedNormals = new float[normals.Length * 3];
        for (int i = 0; i < normals.Length; i++)
        {
            flattenedNormals[3 * i] = normals[i].x;
            flattenedNormals[3 * i + 1] = normals[i].y;
            flattenedNormals[3 * i + 2] = normals[i].z;
        }

        List<int> combinedTriangles = new List<int>();
        combinedTriangles.AddRange(roadTriangles);
        combinedTriangles.AddRange(underRoadTriangles);
        combinedTriangles.AddRange(sideOfRoadTriangles);
        
        SerializableMeshInfo mesh = new SerializableMeshInfo(flattenedVerts, combinedTriangles.ToArray(), flattenedUVs, flattenedNormals);
        return mesh;
    }

    
    protected override void ReleaseData()
    {
        roads = null;
    }
}
