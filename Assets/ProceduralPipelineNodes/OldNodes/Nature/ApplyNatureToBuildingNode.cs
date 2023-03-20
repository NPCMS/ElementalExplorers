using System;
using System.Collections.Generic;
using UnityEngine;
using Utils;
using XNode;
//using UnityEditor;
using Random = UnityEngine.Random;

namespace ProceduralPipelineNodes.Nodes.Nature
{
    [CreateNodeMenu("Legacy/Apply Nature to Building")]
    public class ApplyNatureToBuildingNode : ExtendedNode
    {
        [Input] public GameObject[] buildingToApply;
        [Input] public Mesh mesh;
        [Input] public float density;
        [Input] public Texture2D noiseTexture;
        [Input] public Material buildingMaterial;
        [Input] public Material natureMaterial;
	
        [Output] public Matrix4x4[] transforms;

        // Use this for initialization
        protected override void Init() {
            base.Init();
		
        }
	
        // Return the correct value of an output port when requested
        public override object GetValue(NodePort port) {
            if (port.fieldName == "transforms")
            {
                return transforms;
            }
            return null; // Replace this
        }
    
        private Matrix4x4[] DrawUsingDirectGPUInstancing(GameObject go,
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
                scales.Add(new Vector3(Random.Range(2, 3), Random.Range(2,3), 1.5f));
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

            // check for valid uv
            if (meshUVs.Length != meshVerts.Length)
            {
                // UnwrapParam temp = new UnwrapParam
                // {
                //     areaError = 0.1f,
                //     hardAngle = 60
                // };
                //
                // Vector2[] genenedUVs =
                //     Unwrapping.GeneratePerTriangleUV(filter.sharedMesh, temp);
                // filter.sharedMesh.uv = genenedUVs;
                // meshUVs = genenedUVs;
                return new List<Vector3>();
            }

            // calculate total area of mesh
            // float totalSurfaceArea = 0f;
            // for (int i = 0; i < meshTris.Length; i += 3)
            // {
            //     Vector3[] tri = new Vector3[]
            //     {
            //         meshVerts[meshTris[i]],
            //         meshVerts[meshTris[i + 1]],
            //         meshVerts[meshTris[i + 2]]
            //     };
            //     totalSurfaceArea += BarycentricCoordinates.AreaOf3dTri(tri[0], tri[1], tri[2]);
            // }

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
	
        private void NaturifyGameObject(GameObject go, Texture2D noiseTex, float density, List<Matrix4x4> transformsList)
        {
            // apply new material
            // go.GetComponent<MeshRenderer>().sharedMaterial = buildingMat;
            // generate noise filtered points on mesh
            List<Vector3> noiseFilteredPoints = new List<Vector3>();
            List<Vector3> normalsForPoints = new List<Vector3>();
            MeshFilter[] filters = go.GetComponentsInChildren<MeshFilter>();
            foreach (MeshFilter filter in filters)
            {
                noiseFilteredPoints = GeneratePlacementPointsForAssets(filter, noiseTex, density, normalsForPoints);
                transformsList.AddRange(DrawUsingDirectGPUInstancing(go, noiseFilteredPoints, normalsForPoints));
                noiseFilteredPoints.Clear();
                normalsForPoints.Clear();
            }

            // Different Rendering Methods Below
            // DrawUsingStaticBatching();
        }

        public override void CalculateOutputs(Action<bool> callback)
        {
            Texture2D noiseTex = GetInputValue("noiseTexture", noiseTexture);
            Material buildingMat = GetInputValue("buildingMaterial", buildingMaterial);
            //Material natureMat = GetInputValue("natureMaterial", natureMaterial);
            // pass noise texture to shader for blending
            buildingMat.SetTexture("_NoiseMap", noiseTex);
            float density = GetInputValue("density", this.density);
            //Mesh mesh = GetInputValue("mesh", this.mesh);

            GameObject[] buildings = GetInputValue("buildingToApply", buildingToApply);
            List<Matrix4x4> transformsList = new List<Matrix4x4>();
            for (int i = 0; i < buildings.Length; i++)
            {
                NaturifyGameObject(buildings[i], noiseTex, density, transformsList);
            }

            transforms = transformsList.ToArray();
        
            callback.Invoke(true);
        }

        public override void Release()
        {
            base.Release();
            buildingToApply = null;
            transforms = null;
            //noiseTexture = null;
        }
    }
}