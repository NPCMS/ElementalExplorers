using System.Collections;
using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEditor;
using UnityEngine;
using Utils;

public class BuildingNatureEditor : EditorWindow
{
    // game object to apply to
    private Object _targetGameObject;

    // reference to default mat
    private Object _buildingMatRef;

    // primary asset
    private Object _primaryAsset;

    // secondary asset
    private Object _secondaryAsset;

    // mesh and material if using instancing
    private Object _mesh;
    private Object _material;

    // compute shader
    private Object _noiseComputeShader;

    // octaves
    private int _octaves;

    // scale
    private float _scale;

    // persistence
    private float _persistence;

    // lacunarity
    private float _lacunarity;

    // brightness
    private float _brightness;

    // texture to store the noise
    private Texture2D _noiseTex;

    // asset density
    private int _densityOfAssetsPlaced = 100;

    // list of placement points
    private List<Vector3> _noiseFilteredPoints;

    // corresponding list of normals
    private List<Vector3> _normalsForPoints;

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
        _targetGameObject = EditorGUILayout.ObjectField(_targetGameObject, typeof(GameObject), true);
        GUILayout.Label("Nature Building Material", EditorStyles.boldLabel);
        _buildingMatRef = EditorGUILayout.ObjectField(_buildingMatRef, typeof(Material), true);
        // noise stuff
        GUILayout.Label("Noise Compute Shader", EditorStyles.boldLabel);
        _noiseComputeShader = EditorGUILayout.ObjectField(_noiseComputeShader, typeof(ComputeShader), true);
        GUILayout.Label("octaves", EditorStyles.boldLabel);
        _octaves = EditorGUILayout.IntSlider(_octaves, 1, 6);
        GUILayout.Label("scale", EditorStyles.boldLabel);
        _scale = EditorGUILayout.Slider(_scale, 0, 10);
        GUILayout.Label("lacunarity", EditorStyles.boldLabel);
        _lacunarity = EditorGUILayout.Slider(_lacunarity, 0, 1);
        GUILayout.Label("persistence", EditorStyles.boldLabel);
        _persistence = EditorGUILayout.Slider(_persistence, 0, 1);
        GUILayout.Label("brightness", EditorStyles.boldLabel);
        _brightness = EditorGUILayout.Slider(_brightness, 0, 1);

        // assets stuff
        GUILayout.Label("Primary Asset", EditorStyles.boldLabel);
        _primaryAsset = EditorGUILayout.ObjectField(_primaryAsset, typeof(GameObject), true);
        GUILayout.Label("Secondary Asset", EditorStyles.boldLabel);
        _secondaryAsset = EditorGUILayout.ObjectField(_secondaryAsset, typeof(GameObject), true);
        // instancing stuff
        GUILayout.Label("If using instancing set mesh and material", EditorStyles.largeLabel);
        _mesh = EditorGUILayout.ObjectField(_mesh, typeof(Mesh), true);
        _material = EditorGUILayout.ObjectField(_material, typeof(Material), true);
        
        GUILayout.Label("Points to scatter (entire mesh before accounting for noise)", EditorStyles.boldLabel);
        _densityOfAssetsPlaced = EditorGUILayout.IntSlider(_densityOfAssetsPlaced, 1, 50000);

        if (GUILayout.Button("Run"))
        {
            RunEditorPipeline();
        }

        if (_noiseTex)
        {
            EditorGUI.DrawPreviewTexture(new Rect(25, 450, 200, 200), _noiseTex);
        }
    }


    private void RunEditorPipeline()
    {
        // init variables
        _noiseFilteredPoints = new List<Vector3>();
        _normalsForPoints = new List<Vector3>();

        // generate noise texture
        _noiseTex = GenerateNoiseTex();
        // create new material for each object
        Material buildingMat = new Material((Material) _buildingMatRef);
        // pass noise texture to shader for blending
        buildingMat.SetTexture("_NoiseMap", _noiseTex);
        // apply new material
        _targetGameObject.GetComponent<MeshRenderer>().material = buildingMat;
        // generate noise filtered points on mesh
        _noiseFilteredPoints = GeneratePlacementPointsForAssets();

        // Different Rendering Methods Below
        // DrawUsingStaticBatching();
        DrawUsingDirectGPUInstancing();
    }

    
    private void DrawUsingDirectGPUInstancing()
    {
        Debug.Log(_noiseFilteredPoints.Count);
        // create points in instancing readable format
        List<Vector3> positions = new List<Vector3>();
        List<Vector3> rotations = new List<Vector3>();
        List<Vector3> scales = new List<Vector3>();
        // get ref to parent
        Transform transformRef = _targetGameObject.GetComponent<Transform>();
        // create throwaway transform for angle maths
        GameObject temp = new GameObject();
        
        for (var index = 0; index < _noiseFilteredPoints.Count; index++)
        {
            // compute pos
            positions.Add(Vector3.Scale(_noiseFilteredPoints[index], transformRef.localScale) + transformRef.position);
            // set scale
            scales.Add(new Vector3(1f, 1f, 1f));
            // compute rotation
            temp.transform.up = _normalsForPoints[index];
            rotations.Add(temp.transform.rotation.eulerAngles);
        }
        // destroy gameObject temp
        DestroyImmediate(temp);        
        // create counter to prevent out of bounds on list
        int count = 1023;

        // direct instancing can only handle 1023 meshes so split into several instancer
        for (int i = 0; i < _noiseFilteredPoints.Count; i += 1023)
        {
            // check if count needs updating
            if (_noiseFilteredPoints.Count - i < 1023)
            {
                count = _noiseFilteredPoints.Count - i;
            }
            
            // create game object to hold instancer
            GameObject instancer = new GameObject("instancer");
            instancer.transform.parent = _targetGameObject.GetComponent<Transform>();
            DrawMeshInstancedDirect instancerScript = instancer.AddComponent<DrawMeshInstancedDirect>();
            instancerScript.Setup(
                count,
                (Material)_material,
                (Mesh)_mesh,
                positions.GetRange(i, count),
                rotations.GetRange(i, count),
                scales.GetRange(i, count)
            );
        }
    }
    
    private void DrawUsingStaticBatching()
    {
        // create parent for nature assets
        GameObject natureParent = new GameObject();
        natureParent.name = "natureParent";
        Transform natureParentTransform = natureParent.GetComponent<Transform>();
        natureParentTransform.parent = _targetGameObject.GetComponent<Transform>();
        // draw assets on points
        for (var i = 0; i < _noiseFilteredPoints.Count; i++)
        {
            Vector3 point = _noiseFilteredPoints[i];
            Vector3 normal = _normalsForPoints[i];
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
        Transform transformRef = _targetGameObject.GetComponent<Transform>();
        // instantiate primary or secondary asset, the greater than determines the split between primary and secondary
        GameObject temp;
        if (Random.Range(0.0f, 1.0f) > 0.5f)
        {
            temp = Instantiate((GameObject) _primaryAsset);
        }
        else
        {
            temp = Instantiate((GameObject) _secondaryAsset);
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
        temp.transform.localScale = new Vector3(1f * randScale, 1f * randScale, 1f * randScale);
        // set pos
        temp.transform.position = Vector3.Scale(point, transformRef.localScale) + transformRef.position;
        // inset point
        temp.transform.position = temp.transform.position - (0f * axis);
    }

    private List<Vector3> GeneratePlacementPointsForAssets()
    {
        // list of final filtered points
        List<Vector3> filteredPoints = new List<Vector3>();
        // tris are a list of refs to verts
        int[] meshTris = _targetGameObject.GetComponent<MeshFilter>().sharedMesh.triangles;
        // verts are points in 3d space
        Vector3[] meshVerts = _targetGameObject.GetComponent<MeshFilter>().sharedMesh.vertices;
        // UVs are indexed in the same order as points and reference a 2d textureCoord
        Vector2[] meshUVs = _targetGameObject.GetComponent<MeshFilter>().sharedMesh.uv;
        // normals are indexed the same
        Vector3[] meshNormals = _targetGameObject.GetComponent<MeshFilter>().sharedMesh.normals;

        // check for valid uv
        if (meshUVs.Length != meshVerts.Length)
        {
            UnwrapParam temp = new UnwrapParam
            {
                areaError = 0.1f,
                hardAngle = 60
            };

            Vector2[] genenedUVs =
                Unwrapping.GeneratePerTriangleUV(_targetGameObject.GetComponent<MeshFilter>().sharedMesh, temp);
            _targetGameObject.GetComponent<MeshFilter>().sharedMesh.uv = genenedUVs;
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

            // create tangents tri
            Vector3[] tangentsTri = new Vector3[]
            {
                meshNormals[meshTris[i]],
                meshNormals[meshTris[i + 1]],
                meshNormals[meshTris[i + 2]]
            };
            // compute number of points to sample as tri area / total area
            float triArea = BarycentricCoordinates.AreaOf3dTri(tri[0], tri[1], tri[2]);
            int numberOfPoints = Mathf.RoundToInt((triArea / totalSurfaceArea) * _densityOfAssetsPlaced);
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
                    _noiseTex.GetPixel(
                        (int) (textureCoordsForPoint[0] * _noiseTex.width),
                        (int) (textureCoordsForPoint[1] * _noiseTex.height)).r;
                // if noise at point is over threshold then allow point
                if (noiseValAtPixel > 0.51f)
                {
                    // add points
                    filteredPoints.Add(point);
                    // compute normal
                    Vector3 normal = barycentricParams[0] * normalsTri[0] +
                                     barycentricParams[1] * normalsTri[1] +
                                     barycentricParams[2] * normalsTri[2];
                    _normalsForPoints.Add(normal);
                }
            }
        }

        // return points filtered to be applicable to mesh
        return filteredPoints;
    }

    private Texture2D GenerateNoiseTex()
    {
        ((ComputeShader) _noiseComputeShader).SetFloat("_Scale", _scale);
        ((ComputeShader) _noiseComputeShader).SetVector("_Offset", new Vector2(0, 0));

        return TextureGenerator.RenderComputeShader(1024, 1024, (ComputeShader) _noiseComputeShader, _brightness,
            _octaves, _lacunarity, _persistence);
    }
}