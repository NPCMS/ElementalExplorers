using System;
using System.Collections.Generic;
using UnityEngine;
using Random = UnityEngine.Random;
using Utils;
using XNode;
using UnityEditor;

[CreateNodeMenu("Nature/Apply Nature To Buildings")]
public class ApplyNatureToBuildingNode : AsyncExtendedNode {
    [Input] public GameObjectData[] buildingToApply;
    //[Input] public Mesh mesh;
    [Input] public float density;
    [Input] public float scaleFactor;
    [Input] public TextureWrapAsync noiseTexture;
    [Input] public int noiseTextureWidth;
    //[Input] public Material buildingMaterial;
    //[Input] public Material natureMaterial;

    [Output] public Matrix4x4[] transforms;

    // Use this for initialization
    protected override void Init()
    {
        base.Init();

    }

    // Return the correct value of an output port when requested
    public override object GetValue(NodePort port)
    {
        if (port.fieldName == "transforms")
        {
            return transforms;
        }
        return null; // Replace this
    }

    private Matrix4x4[] DrawUsingDirectGPUInstancing(GameObject go, float scale,
        List<Vector3> noiseFilteredPoints,
        List<Vector3> normalsForPoints)
    {
        // create points in instancing readable format
        List<Vector3> positions = new List<Vector3>();
        List<Vector3> rotations = new List<Vector3>();
        List<Vector3> scales = new List<Vector3>();
        // get ref to parent
        Transform transformRef = go.transform;
        // create throwaway transform for angle maths
        GameObject temp = new GameObject();

        for (var index = 0; index < noiseFilteredPoints.Count; index++)
        {
            // compute pos
            positions.Add(Vector3.Scale(noiseFilteredPoints[index], transformRef.localScale) + transformRef.position);
            // set scale
            scales.Add(new Vector3(Random.Range(2, 3), Random.Range(2, 3), 1.5f) * scale);
            // compute rotation
            temp.transform.up = normalsForPoints[index];
            // add random rotation
            temp.transform.Rotate(normalsForPoints[index], Random.Range(-180, 180));
            rotations.Add(temp.transform.rotation.eulerAngles);
        }
        // destroy gameObject temp
        DestroyImmediate(temp);
        Matrix4x4[] output = new Matrix4x4[positions.Count];
        for (int i = 0; i < positions.Count; i++)
        {
            output[i] = Matrix4x4.TRS(positions[i], Quaternion.Euler(rotations[i]), scales[i]);
        }

        return output;
    }


    private Matrix4x4[] DrawUsingDirectGPUInstancingAsync(Vector3 pos, float scale,
        List<Vector3> noiseFilteredPoints,
        List<Vector3> normalsForPoints)
    {
        // create points in instancing readable format
        List<Vector3> positions = new List<Vector3>();
        List<Quaternion> rotations = new List<Quaternion>();
        List<Vector3> scales = new List<Vector3>();
        // get ref to parent

        System.Random rnd = new System.Random();

        for (var index = 0; index < noiseFilteredPoints.Count; index++)
        {
            // compute pos
            positions.Add(noiseFilteredPoints[index] + pos);
            // set scale
            float scaleX = 2 + (float)rnd.NextDouble();
            float scaleY = 2 + (float)rnd.NextDouble();
            scales.Add(new Vector3(scaleX, scaleY, 1.5f) * scale);
            // compute rotation
            Vector3 up = normalsForPoints[index];
            Vector3 forward = Vector3.Cross(up, Vector3.right);

            Quaternion rotation = Quaternion.AngleAxis((float)rnd.NextDouble() * 360.0f, up) * Quaternion.LookRotation(forward, up);
            rotations.Add(rotation);
        }
        Matrix4x4[] output = new Matrix4x4[positions.Count];
        for (int i = 0; i < positions.Count; i++)
        {
            output[i] = Matrix4x4.TRS(positions[i], rotations[i], scales[i]);
        }

        return output;
    }
    private List<Vector3> GeneratePlacementPointsForAssets(MeshFilter filter, Texture2D noiseTex, float density, List<Vector3> normalsForPoints)
    {
        // list of final filtered points
        List<Vector3> filteredPoints = new List<Vector3>();
        // tris are a list of refs to verts
        int[] meshTris = filter.sharedMesh.triangles;
        // verts are points in 3d space
        Vector3[] meshVerts = filter.sharedMesh.vertices;
        // UVs are indexed in the same order as points and reference a 2d textureCoord
        Vector2[] meshUVs = filter.sharedMesh.uv;
        // normals are indexed the same
        Vector3[] meshNormals = filter.sharedMesh.normals;

        if (meshUVs.Length != meshVerts.Length)
        {
            return new List<Vector3>();
        }

        // iterate tris
        for (int i = 0; i < meshTris.Length; i += 3)
        {
            // create tri, meshTris holds indexes of vertices in meshVerts
            Vector3[] tri = new Vector3[]
            {
                    meshVerts[meshTris[i]],
                    meshVerts[meshTris[i + 1]],
                    meshVerts[meshTris[i + 2]]
            };

            // create UV tri
            Vector2[] uvTri = new Vector2[]
            {
                    meshUVs[meshTris[i]],
                    meshUVs[meshTris[i + 1]],
                    meshUVs[meshTris[i + 2]]
            };

            // create normals tri
            Vector3[] normalsTri = new Vector3[]
            {
                    meshNormals[meshTris[i]],
                    meshNormals[meshTris[i + 1]],
                    meshNormals[meshTris[i + 2]]
            };

            // compute number of points to sample as tri area / total area
            float triArea = BarycentricCoordinates.AreaOf3dTri(tri[0], tri[1], tri[2]);
            int numberOfPoints = Mathf.RoundToInt(triArea * density);
            // sample some random points
            List<Vector3> unfilteredPoints = SampleRandomPointsOnTri.SampleRandPointsOnTri(numberOfPoints, tri);
            // filter points based on noise
            foreach (Vector3 point in unfilteredPoints)
            {
                // compute barycentric params for point
                float[] barycentricParams = BarycentricCoordinates.ComputeThreeDimensionalBarycentricCoords(tri, point);

                // sample noise texture using UVs
                Vector2 textureCoordsForPoint = barycentricParams[0] * uvTri[0] +
                                                barycentricParams[1] * uvTri[1] +
                                                barycentricParams[2] * uvTri[2];
                // sample texture to get noise value at point
                float noiseValAtPixel =
                    noiseTex.GetPixel(
                        (int)(textureCoordsForPoint[0] * noiseTex.width),
                        (int)(textureCoordsForPoint[1] * noiseTex.height)).r;
                // if noise at point is over threshold then allow point
                if (noiseValAtPixel > 0.51f)
                {
                    // add points
                    filteredPoints.Add(point);
                    // compute normal
                    Vector3 normal = barycentricParams[0] * normalsTri[0] +
                                     barycentricParams[1] * normalsTri[1] +
                                     barycentricParams[2] * normalsTri[2];
                    normalsForPoints.Add(normal);
                }
            }
        }

        // return points filtered to be applicable to mesh
        return filteredPoints;
    }
    private List<Vector3> GeneratePlacementPointsForAssetsAsync(SerializableMeshInfo mesh, float[,] noiseTex, int width, float density, List<Vector3> normalsForPoints)
    {
        // list of final filtered points
        List<Vector3> filteredPoints = new List<Vector3>();
        mesh.GetMeshComponents(out Vector3[] meshVerts, out int[] meshTris, out Vector3[] meshNormals, out Vector2[] meshUVs);

        if (meshUVs.Length != meshVerts.Length)
        {
            return new List<Vector3>();
        }

        // iterate tris
        for (int i = 0; i < meshTris.Length; i += 3)
        {
            // create tri, meshTris holds indexes of vertices in meshVerts
            Vector3[] tri = new Vector3[]
            {
                    meshVerts[meshTris[i]],
                    meshVerts[meshTris[i + 1]],
                    meshVerts[meshTris[i + 2]]
            };

            // create UV tri
            Vector2[] uvTri = new Vector2[]
            {
                    meshUVs[meshTris[i]],
                    meshUVs[meshTris[i + 1]],
                    meshUVs[meshTris[i + 2]]
            };

            // create normals tri
            Vector3[] normalsTri = new Vector3[]
            {
                    meshNormals[meshTris[i]],
                    meshNormals[meshTris[i + 1]],
                    meshNormals[meshTris[i + 2]]
            };

            // compute number of points to sample as tri area / total area
            float triArea = BarycentricCoordinates.AreaOf3dTri(tri[0], tri[1], tri[2]);
            int numberOfPoints = Mathf.RoundToInt(triArea * density);
            // sample some random points
            System.Random rnd = new System.Random();
            List<Vector3> unfilteredPoints = SampleRandomPointsOnTri.SampleRandPointsOnTriAsync(numberOfPoints, tri, rnd);
            // filter points based on noise
            foreach (Vector3 point in unfilteredPoints)
            {
                // compute barycentric params for point
                float[] barycentricParams = BarycentricCoordinates.ComputeThreeDimensionalBarycentricCoords(tri, point);

                // sample noise texture using UVs
                Vector2 textureCoordsForPoint = barycentricParams[0] * uvTri[0] +
                                                barycentricParams[1] * uvTri[1] +
                                                barycentricParams[2] * uvTri[2];
                // sample texture to get noise value at point
                float noiseValAtPixel = noiseTex[(int)(Mathf.Abs(textureCoordsForPoint[0] * width) % width), (int)(Mathf.Abs(textureCoordsForPoint[1] * width) % width)];
                // if noise at point is over threshold then allow point
                if (noiseValAtPixel > 0.51f)
                {
                    // add points
                    filteredPoints.Add(point);
                    // compute normal
                    Vector3 normal = barycentricParams[0] * normalsTri[0] +
                                     barycentricParams[1] * normalsTri[1] +
                                     barycentricParams[2] * normalsTri[2];
                    normalsForPoints.Add(normal);
                }
            }
        }

        // return points filtered to be applicable to mesh
        return filteredPoints;
    }

    //private void NaturifyGameObject(GameObject go, Texture2D noiseTex, float density, List<Matrix4x4> transformsList)
    //{
    //    // apply new material
    //    // go.GetComponent<MeshRenderer>().sharedMaterial = buildingMat;
    //    // generate noise filtered points on mesh
    //    List<Vector3> noiseFilteredPoints = new List<Vector3>();
    //    List<Vector3> normalsForPoints = new List<Vector3>();
    //    MeshFilter[] filters = go.GetComponentsInChildren<MeshFilter>();
    //    foreach (MeshFilter filter in filters)
    //    {
    //        noiseFilteredPoints = GeneratePlacementPointsForAssets(filter, noiseTex, density, normalsForPoints);
    //        transformsList.AddRange(DrawUsingDirectGPUInstancing(go, noiseFilteredPoints, normalsForPoints));
    //        noiseFilteredPoints.Clear();
    //        normalsForPoints.Clear();
    //    }

    //    // Different Rendering Methods Below
    //    // DrawUsingStaticBatching();
    //}

    private void NaturifyGameObjectAsync(MeshGameObjectData go, float[,] noiseTex, int width, float density, float scale, List<Matrix4x4> transformsList)
    {
        List<Vector3> normalsForPoints = new List<Vector3>();
        //MeshFilter[] filters = go.GetComponentsInChildren<MeshFilter>();
        List<Vector3> noiseFilteredPoints = GeneratePlacementPointsForAssetsAsync(go.Mesh, noiseTex, width, density, normalsForPoints);
        transformsList.AddRange(DrawUsingDirectGPUInstancingAsync(go.Position, scale, noiseFilteredPoints, normalsForPoints));
    }


    //   public override IEnumerator CalculateOutputs(Action<bool> callback)
    //   {
    //       SyncYieldingWait wait = new SyncYieldingWait();
    //       Texture2D noiseTex = GetInputValue("noiseTexture", noiseTexture);
    //       //Material buildingMat = GetInputValue("buildingMaterial", buildingMaterial);
    //       //Material natureMat = GetInputValue("natureMaterial", natureMaterial);
    //       // pass noise texture to shader for blending
    //       //buildingMat.SetTexture("_NoiseMap", noiseTex);
    //       float density = GetInputValue("density", this.density);
    //       //Mesh mesh = GetInputValue("mesh", this.mesh);

    //       GameObject[] buildings = GetInputValue("buildingToApply", buildingToApply);
    //       List<Matrix4x4> transformsList = new List<Matrix4x4>();
    //       for (int i = 0; i < buildings.Length; i++)
    //       {
    //           NaturifyGameObject(buildings[i], noiseTex, density, transformsList);
    //           if (wait.YieldIfTimePassed())
    //           {
    //               yield return null;
    //           }
    //       }

    //       transforms = transformsList.ToArray();

    //       callback.Invoke(true);
    //   }

    //public override void Release()
    //   {
    //       buildingToApply = null;
    //       transforms = null;
    //   }

    protected override void CalculateOutputsAsync(Action<bool> callback)
    {
        float[,] noiseTex = GetInputValue("noiseTexture", noiseTexture).tex;
        int width = GetInputValue("noiseTextureWidth", noiseTextureWidth);
        //Material buildingMat = GetInputValue("buildingMaterial", buildingMaterial);
        //Material natureMat = GetInputValue("natureMaterial", natureMaterial);
        // pass noise texture to shader for blending
        //buildingMat.SetTexture("_NoiseMap", noiseTex);
        float density = GetInputValue("density", this.density);
        float scale = GetInputValue("scaleFactor", scaleFactor);
        //Mesh mesh = GetInputValue("mesh", this.mesh);

        GameObjectData[] buildings = GetInputValue("buildingToApply", buildingToApply);
        List<Matrix4x4> transformsList = new List<Matrix4x4>();
        for (int i = 0; i < buildings.Length; i++)
        {
            if (buildings[i] is MeshGameObjectData)
            {
                NaturifyGameObjectAsync((MeshGameObjectData)buildings[i], noiseTex, width, density, scale, transformsList);
            }
        }

        transforms = transformsList.ToArray();

        callback.Invoke(true);
    }

    protected override void ReleaseData()
    {
        buildingToApply = null;
        transforms = null;
        noiseTexture = null;
    }
    public override void ApplyGUI()
    {
        base.ApplyGUI();
#if UNITY_EDITOR
        EditorGUILayout.LabelField($"{transforms.Length} transforms");
#endif
    }
}