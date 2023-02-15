using System.Collections;
using System.Collections.Generic;
using Codice.Client.BaseCommands;
using Unity.VisualScripting;
using UnityEditor;
using UnityEngine;
using Utils;

public class BuildingNatureEditor : EditorWindow
{
    // game object to apply to
    private Object targetGameObject;

    // reference to default mat
    private Object buildingMatRef;

    // primary asset
    private Object primaryAsset;

    // secondary asset
    private Object secondaryAsset;

    // compute shader
    private Object noiseComputeShader;

    // octaves
    private int octaves;

    // scale
    private float scale;

    // persistence
    private float persistence;

    // lacunarity
    private float lacunarity;

    // brightness
    private float brightness;

    // texture to store the noise
    private Texture2D noiseTex;

    // asset density
    private int densityOfAssetsPlaced = 100;

    // list of placement points
    private List<Vector3> noiseFilteredPoints;

    // corresponding list of normals
    private List<Vector3> normalsForPoints;

    // Add menu named "My Window" to the Window menu
    [MenuItem("Nature Tools/Building Nature")]
    static void Init()
    {
        // Get existing open window or if none, make a new one:
        BuildingNatureEditor window = (BuildingNatureEditor) EditorWindow.GetWindow(typeof(BuildingNatureEditor));
        window.Show();
    }

    void OnGUI()
    {
        GUILayout.Label("Target Object", EditorStyles.boldLabel);
        targetGameObject = EditorGUILayout.ObjectField(targetGameObject, typeof(GameObject), true);
        GUILayout.Label("Nature Building Material", EditorStyles.boldLabel);
        buildingMatRef = EditorGUILayout.ObjectField(buildingMatRef, typeof(Material), true);
        // noise stuff
        GUILayout.Label("Noise Compute Shader", EditorStyles.boldLabel);
        noiseComputeShader = EditorGUILayout.ObjectField(noiseComputeShader, typeof(ComputeShader), true);
        GUILayout.Label("octaves", EditorStyles.boldLabel);
        octaves = EditorGUILayout.IntSlider(octaves, 1, 6);
        GUILayout.Label("scale", EditorStyles.boldLabel);
        scale = EditorGUILayout.Slider(scale, 0, 10);
        GUILayout.Label("lacunarity", EditorStyles.boldLabel);
        lacunarity = EditorGUILayout.Slider(lacunarity, 0, 1);
        GUILayout.Label("persistence", EditorStyles.boldLabel);
        persistence = EditorGUILayout.Slider(persistence, 0, 1);
        GUILayout.Label("brightness", EditorStyles.boldLabel);
        brightness = EditorGUILayout.Slider(brightness, 0, 1);

        // assets stuff
        GUILayout.Label("Primary Asset", EditorStyles.boldLabel);
        primaryAsset = EditorGUILayout.ObjectField(primaryAsset, typeof(GameObject), true);
        GUILayout.Label("Secondary Asset", EditorStyles.boldLabel);
        secondaryAsset = EditorGUILayout.ObjectField(secondaryAsset, typeof(GameObject), true);
        GUILayout.Label("Points to scatter (entire mesh before accounting for noise)", EditorStyles.boldLabel);
        densityOfAssetsPlaced = EditorGUILayout.IntSlider(densityOfAssetsPlaced, 1, 50000);

        if (GUILayout.Button("Run"))
        {
            RunEditorPipeline();
        }

        if (noiseTex)
        {
            EditorGUI.DrawPreviewTexture(new Rect(25, 450, 200, 200), noiseTex);
        }
    }

    private void RunEditorPipeline()
    {
        // init variables
        noiseFilteredPoints = new List<Vector3>();
        normalsForPoints = new List<Vector3>();
        
        // generate noise texture
        noiseTex = GenerateNoiseTex();
        // create new material for each object
        Material buildingMat = new Material((Material) buildingMatRef);
        // pass noise texture to shader for blending
        buildingMat.SetTexture("_NoiseMap", noiseTex);
        // apply new material
        targetGameObject.GetComponent<MeshRenderer>().material = buildingMat;
        // generate noise filtered points on mesh
        noiseFilteredPoints = GeneratePlacementPointsForAssets();
        // create parent for nature assets
        GameObject natureParent = new GameObject();
        natureParent.name = "natureParent";
        Transform natureParentTransform = natureParent.GetComponent<Transform>();
        natureParentTransform.parent = targetGameObject.GetComponent<Transform>();
        // draw assets on points
        for (var i = 0; i < noiseFilteredPoints.Count; i++)
        {
            Vector3 point = noiseFilteredPoints[i];
            Vector3 normal = normalsForPoints[i];
            InstantiateAssetOnPoint(point, normal, natureParentTransform);
        }
        // combines the meshes of all children for static batching, not sure about LOD interactions
        // potentially need to setup LOD group on parent with just culling
        StaticBatchingUtility.Combine(natureParent);
    }

    // point is location, axis the new up direction for the mesh and parent is the parent
    private void InstantiateAssetOnPoint(Vector3 point, Vector3 axis, Transform parent)
    {
        // get transform ref
        Transform transformRef = targetGameObject.GetComponent<Transform>();
        // instantiate primary or secondary asset, the greater than determines the split between primary and secondary
        GameObject temp;
        if (Random.Range(0.0f, 1.0f) > 0.5f)
        {
            temp = Instantiate((GameObject) primaryAsset);
        }
        else
        {
            temp = Instantiate((GameObject) secondaryAsset);
        }
        // set parent
        temp.transform.parent = parent;
        // get random scale
        float randScale = Random.Range(0.5f, 1.25f);
        // set rotation to align with normal axis
        temp.transform.up = axis;
        // add random rotation
        temp.transform.Rotate(axis, Random.Range(-50, 50));
        // set scale
        temp.transform.localScale = new Vector3(30f * randScale, 30f * randScale, 30f * randScale);
        // set pos
        temp.transform.position = Vector3.Scale(point, transformRef.localScale) + transformRef.position;
        // inset point
        temp.transform.position = temp.transform.position - (0.1f * axis);
    }

    private List<Vector3> GeneratePlacementPointsForAssets()
    {
        // list of final filtered points
        List<Vector3> filteredPoints = new List<Vector3>();
        // tris are a list of refs to verts
        int[] meshTris = targetGameObject.GetComponent<MeshFilter>().sharedMesh.triangles;
        // verts are points in 3d space
        Vector3[] meshVerts = targetGameObject.GetComponent<MeshFilter>().sharedMesh.vertices;
        // UVs are indexed in the same order as points and reference a 2d textureCoord
        Vector2[] meshUVs = targetGameObject.GetComponent<MeshFilter>().sharedMesh.uv;
        // normals are indexed the same
        Vector3[] meshNormals = targetGameObject.GetComponent<MeshFilter>().sharedMesh.normals;

        // check for valid uv
        if (meshUVs.Length != meshVerts.Length)
        {
            UnwrapParam temp = new UnwrapParam
            {
                areaError = 0.1f,
                hardAngle = 60
            };

            Vector2[] genenedUVs = Unwrapping.GeneratePerTriangleUV(targetGameObject.GetComponent<MeshFilter>().sharedMesh, temp);
            targetGameObject.GetComponent<MeshFilter>().sharedMesh.uv = genenedUVs;
            meshUVs = genenedUVs;
        }
        
        // calculate total area of mesh
        float totalSurfaceArea = 0f;
        for (int i = 0; i < meshTris.Length; i += 3)
        {
            Vector3[] tri = new Vector3[]
            {
                meshVerts[meshTris[i]],
                meshVerts[meshTris[i + 1]],
                meshVerts[meshTris[i + 2]]
            };
            totalSurfaceArea += BarycentricCoordinates.AreaOf3dTri(tri[0], tri[1], tri[2]);
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
            Vector2[] UVTri = new Vector2[]
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
            
            // create tangents tri
            Vector3[] tangentsTri = new Vector3[]
            {
                meshNormals[meshTris[i]],
                meshNormals[meshTris[i + 1]],
                meshNormals[meshTris[i + 2]]
            };
            // compute number of points to sample as tri area / total area
            float triArea = BarycentricCoordinates.AreaOf3dTri(tri[0], tri[1], tri[2]);
            int numberOfPoints = Mathf.RoundToInt((triArea / totalSurfaceArea) * densityOfAssetsPlaced);
            // sample some random points
            List<Vector3> unfilteredPoints = SampleRandomPointsOnTri.SampleRandPointsOnTri(numberOfPoints, tri);
            // filter points based on noise
            foreach (Vector3 point in unfilteredPoints)
            {
                // compute barycentric params for point
                float[] barycentricParams = BarycentricCoordinates.ComputeThreeDimensionalBarycentricCoords(tri, point);

                // sample noise texture using UVs
                Vector2 textureCoordsForPoint = barycentricParams[0] * UVTri[0] +
                                                barycentricParams[1] * UVTri[1] +
                                                barycentricParams[2] * UVTri[2];
                // sample texture to get noise value at point
                float noiseValAtPixel =
                    noiseTex.GetPixel(
                        (int) (textureCoordsForPoint[0] * noiseTex.width),
                        (int) (textureCoordsForPoint[1] * noiseTex.height)).r;
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

    private Texture2D GenerateNoiseTex()
    {
        ((ComputeShader)noiseComputeShader).SetFloat("_Scale", scale);
        ((ComputeShader)noiseComputeShader).SetVector("_Offset", new Vector2(0,0));

        return TextureGenerator.RenderComputeShader(1024, 1024, (ComputeShader)noiseComputeShader, brightness, octaves, lacunarity, persistence);
    }
}