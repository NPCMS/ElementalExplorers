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

    // amount of building reclaimed by nature
    private float natureAmount;

    // scale for noise texture
    private float natureScale;

    // amount of blending between building and nature
    private float natureBlending;

    // texture to store the noise
    private Texture2D noiseTex;

    // size of texture
    int texSize = 2056;

    // max assets to place
    private int densityOfAssetsPlaced = 100;

    // list of placement points
    private List<Vector3> noiseFilteredPoints = new List<Vector3>();


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
        GUILayout.Label("Nature Amount", EditorStyles.boldLabel);
        natureAmount = EditorGUILayout.Slider(natureAmount, 0, 1);
        GUILayout.Label("Nature Scale", EditorStyles.boldLabel);
        natureScale = EditorGUILayout.Slider(natureScale, 0, 0.05f);
        GUILayout.Label("Nature Blending", EditorStyles.boldLabel);
        natureBlending = EditorGUILayout.Slider(natureBlending, 0, 1);
        GUILayout.Label("Points per tri (will be reduced by noise map)", EditorStyles.boldLabel);
        densityOfAssetsPlaced = EditorGUILayout.IntSlider(densityOfAssetsPlaced, 1, 100);

        if (GUILayout.Button("Run"))
        {
            RunEditorPipeline();
        }

        if (noiseTex)
        {
            EditorGUI.DrawPreviewTexture(new Rect(25, 250, 200, 200), noiseTex);
        }
    }

    private void RunEditorPipeline()
    {
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
        // create parent for object
        GameObject parent = new GameObject();
        parent.name = targetGameObject.name + "_parent";
        // make target game object child
        targetGameObject.GetComponent<Transform>().parent = parent.GetComponent<Transform>();
        // create parent for nature assets
        GameObject natureParent = new GameObject();
        natureParent.name = "natureParent";
        Transform natureParentTransform = natureParent.GetComponent<Transform>();
        // draw debug points
        foreach (Vector3 point in noiseFilteredPoints)
        {
            InstantiateTestingPoint(point, natureParentTransform);
        }
    }

    // points is given in UV coords
    private void InstantiateTestingPoint(Vector3 point, Transform parent)
    {
        // get transform ref
        Transform transformRef = targetGameObject.GetComponent<Transform>();
        // create sphere
        GameObject temp = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        // set parent
        temp.transform.parent = parent;
        // set scale
        temp.transform.localScale = new Vector3(0.1f, 0.1f, 0.1f);
        // set pos
        temp.transform.position = Vector3.Scale(point, transformRef.localScale) + transformRef.position;
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
        var meshUVs = targetGameObject.GetComponent<MeshFilter>().sharedMesh.uv;

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

            // sample some random points
            List<Vector3> unfilteredPoints = SampleRandomPointsOnTri.SampleRandPointsOnTri(densityOfAssetsPlaced, tri);
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
                        (int)(textureCoordsForPoint[1] * noiseTex.height)).r;
                // if noise at point is over threshold then allow point
                if (noiseValAtPixel > 0.1f)
                {
                filteredPoints.Add(point);
                }
            }
        }

        // return points filtered to be applicable to mesh
        return filteredPoints;
    }

    private Texture2D GenerateNoiseTex()
    {
        Texture2D temp = new Texture2D(texSize, texSize, TextureFormat.R8, false);
        // float editedBlend = 1 - natureBlending;
        float newBlending = 1 - natureBlending;
        for (int x = 0; x < texSize; x++)
        {
            for (int y = 0; y < texSize; y++)
            {
                float val = Mathf.PerlinNoise(x * (natureScale / 10.0f), y * (natureScale / 10.0f));
                val = Mathf.SmoothStep(natureAmount, natureAmount - newBlending, val);
                temp.SetPixel(x, y, new Color(val, val, val));
            }
        }

        temp.Apply();
        return temp;
    }
}