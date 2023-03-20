using System;
using System.Collections.Generic;
using System.Linq;
using PathCreation;
using QuikGraph;
using UnityEngine;
using XNode;
using Random = System.Random;
using RoadNetworkGraph = QuikGraph.UndirectedGraph<ProceduralPipelineNodes.Nodes.Roads.RoadNetworkNode, QuikGraph.TaggedEdge<ProceduralPipelineNodes.Nodes.Roads.RoadNetworkNode, ProceduralPipelineNodes.Nodes.Roads.RoadNetworkEdge>>;

namespace ProceduralPipelineNodes.Nodes.Roads
{
    [CreateNodeMenu("Roads/Generate OSM Road GameObjects")]
    public class GenerateOSMRoadsGameObjectsNode : ExtendedNode
    {
        [Input] public RoadNetworkGraph networkGraph;
        [Input] public Material material;
        [Input] public Shader roadShader;
        [Input] public ElevationData elevationData;
        [Input] public GlobeBoundingBox boundingBox;

        [Output] public GameObject[] roadsGameObjects;

        // Road snapping layer mask
        private int snappingMask = (1 << 7);

        //Terrain terrain = FindObjectOfType<Terrain>();

        // reference to shader property
        private static readonly int NumberOfDashes = Shader.PropertyToID("_Number_Of_Dashes");

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
            RoadNetworkGraph roadsGraph = GetInputValue("networkGraph", networkGraph).Clone();
            GlobeBoundingBox bb = GetInputValue("boundingBox", boundingBox);
            List<OSMRoadsData> roads = GetRoadsFromGraph(roadsGraph, bb);
            Debug.Log("Created " + roads.Count + " roads");
            ElevationData elevation = GetInputValue("elevationData", elevationData);
        
            // create parent game object
            GameObject roadsParent = new GameObject("Roads");

            // setup outputs

            Material mat = GetInputValue("material", material);

            Random random = new Random(0);
        
            // iterate through road classes
            List<GameObject> gameObjects = new List<GameObject>();
            foreach (OSMRoadsData road in roads)
            {
                var roadDeltaHeight = random.NextDouble() / 100;
                GameObject roadGo = CreateGameObjectFromRoadData(road, roadsParent.transform, mat, elevation, (float)roadDeltaHeight);
                if (roadGo == null) continue;
                gameObjects.Add(roadGo);
            }

            roadsGameObjects = gameObjects.ToArray();
            callback.Invoke(true);
        }

        // gets a node list from a graph. This modifies the given graph and will remove all edges. Could be expensive so might be worth running on a different thread
        private static List<OSMRoadsData> GetRoadsFromGraph(RoadNetworkGraph roadsGraph, GlobeBoundingBox bb)
        {
            List<OSMRoadsData> roads = new List<OSMRoadsData>();
            while (roadsGraph.EdgeCount > 0) // keep adding roads to the roads list until all roads are added
            {
                List<TaggedEdge<RoadNetworkNode, RoadNetworkEdge>> path = new List<TaggedEdge<RoadNetworkNode, RoadNetworkEdge>>();
                // picks the edge with the largest length to start with
                var e = roadsGraph.Edges.First(rd => Math.Abs(rd.Tag.length - roadsGraph.Edges.Max(r => r.Tag.length)) < 0.01);
                path.Add(e);
                var n1 = e.Source;
                while (true) // greedily expand in the n1 direction
                {
                    List<TaggedEdge<RoadNetworkNode, RoadNetworkEdge>> nextEdges = roadsGraph.AdjacentEdges(n1).ToList();
                
                    Vector2 prevDirection = GetDirectionOfRoadEnd(path[0], n1); // vector pointing in the direction of the previous edges end

                    TaggedEdge<RoadNetworkNode, RoadNetworkEdge> bestNextEdge = null;
                    float bestEdgeAngle = float.MaxValue; // picks the edge which will have the angle closest to 0 turning 
                    foreach (var edge in nextEdges)
                    {
                        if (path.Contains(edge)) continue; // stops infinite cycles
                        if (edge.Tag.type.type != path[0].Tag.type.type) continue; // change in road type
                        Vector2 nextDirection = -GetDirectionOfRoadEnd(edge, n1);
                        float roadAngle = Vector2.Angle(prevDirection, nextDirection);
                        if (roadAngle < bestEdgeAngle)
                        {
                            bestEdgeAngle = roadAngle;
                            bestNextEdge = edge;
                        }
                    }
                    if (bestNextEdge == null) break; // there are no edges that can be followed. It's expanded as far as possible
                
                    path.Insert(0, bestNextEdge); // expands the front of the list
                    n1 = bestNextEdge.Target.Equals(n1) ? bestNextEdge.Source : bestNextEdge.Target;
                }
                var n2 = e.Target;
                while (true) // greedily expand in the n2 direction
                {
                    List<TaggedEdge<RoadNetworkNode, RoadNetworkEdge>> nextEdges = roadsGraph.AdjacentEdges(n2).ToList();
                
                    Vector2 prevDirection = GetDirectionOfRoadEnd(path[^1], n2); // vector pointing in the direction of the previous edges end

                    TaggedEdge<RoadNetworkNode, RoadNetworkEdge> bestNextEdge = null;
                    float bestEdgeAngle = float.MaxValue; // picks the edge which will have the angle closest to 0 turning 
                    foreach (var edge in nextEdges)
                    {
                        if (path.Contains(edge)) continue; // stops infinite cycles
                        if (edge.Tag.type.type != path[^1].Tag.type.type) continue; // change in road type
                        Vector2 nextDirection = -GetDirectionOfRoadEnd(edge, n2);
                        float roadAngle = Vector2.Angle(prevDirection, nextDirection);
                        if (roadAngle < bestEdgeAngle)
                        {
                            bestEdgeAngle = roadAngle;
                            bestNextEdge = edge;
                        }
                    }
                    if (bestNextEdge == null) break; // there are no edges that can be followed. It's expanded as far as possible
                
                    path.Add(bestNextEdge); // expands the end of the list
                    n2 = bestNextEdge.Target.Equals(n2) ? bestNextEdge.Source : bestNextEdge.Target;
                }
            
                // path is created. Remove it from the graph
                roadsGraph.RemoveEdges(path);
                // Now convert the path into a road
                List<Vector2> footprint = new List<Vector2>();

                if (path.Count == 1)
                {
                    List<Vector2> road = new List<Vector2>();
                    road.Add(path[0].Source.location);
                    road.AddRange(path[0].Tag.edgePoints);
                    road.Add(path[0].Target.location);
                    roads.Add(new OSMRoadsData(road));
                    continue;
                }
            
                // joins edges into a single footprint for drawing
                var prevEdge = path[0];
                for (int i = 1; i < path.Count - 1; i++)
                {
                    var currentEdge = path[i];
                    if (prevEdge.Source.Equals(currentEdge.Source) || prevEdge.Source.Equals(currentEdge.Target))
                    { // add target edge followed by edge locations in reverse order
                        footprint.Add(prevEdge.Target.location);
                        footprint.AddRange(prevEdge.Tag.edgePoints.Reverse());
                    }
                    else // add source edge followed by edge locations
                    {
                        footprint.Add(prevEdge.Source.location);
                        footprint.AddRange(prevEdge.Tag.edgePoints);
                    }
                    prevEdge = currentEdge;
                }
                // add final edge to the footprint followed by the final node
                if (path[^1].Source.Equals(path[^2].Source) || path[^1].Source.Equals(path[^2].Target))
                { // add edge locations followed by target
                    footprint.Add(path[^1].Source.location);
                    footprint.AddRange(path[^1].Tag.edgePoints);
                    footprint.Add(path[^1].Target.location);
                }
                else // add edge locations in reverse order followed by source location
                {
                    footprint.Add(path[^1].Target.location);
                    footprint.AddRange(path[^1].Tag.edgePoints.Reverse());
                    footprint.Add(path[^1].Source.location);
                }
            
                // convert footprint into world space
                for (int i = 0; i < footprint.Count; i++)
                {
                    footprint[i] = bb.ConvertGeoCoordToMeters(footprint[i]);
                }
                roads.Add(new OSMRoadsData(footprint));
            }
            return roads;
        }

        private static Vector2 GetDirectionOfRoadEnd(TaggedEdge<RoadNetworkNode, RoadNetworkEdge> edge, RoadNetworkNode toNode)
        {
            if (edge.Tag.edgePoints.Length > 0) // if there are nodes between the source and target
            {
                if (edge.Source.Equals(toNode)) // get vector from first edge point to source
                {
                    return edge.Source.location - edge.Tag.edgePoints[0];
                }
                // get vector from last edge point to target
                return edge.Target.location - edge.Tag.edgePoints[^1];
            }

            if (edge.Source.Equals(toNode)) // get vector from target to source
            {
                return edge.Source.location - edge.Target.location;
            }
            // get vector from source to target
            return edge.Target.location - edge.Source.location;
        }

        //credit to Sebastian Lague
        private Mesh CreateRoadMesh(VertexPath path)
        {
            const float roadWidth = 4f;
            const float thickness = 0f;
            const bool flattenSurface = false;
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

            Mesh mesh = new Mesh
            {
                vertices = verts,
                uv = uvs,
                normals = normals,
                subMeshCount = 3
            };
            mesh.SetTriangles(roadTriangles, 0);
            mesh.SetTriangles(underRoadTriangles, 1);
            mesh.SetTriangles(sideOfRoadTriangles, 2);
            mesh.RecalculateBounds();
            return mesh;
        }


        private GameObject CreateGameObjectFromRoadData(OSMRoadsData roadData, Transform parent, Material mat, ElevationData elevation, float deltaHeight)
        {
            Vector2[] vertices = roadData.footprint.ToArray();
            Vector3[] vertices3D = new Vector3[vertices.Length];
            float roadLength = 0f;
            for (int j = 0; j < vertices.Length; j++)
            {
                vertices3D[j] = new Vector3(vertices[j].x, 0.5f, vertices[j].y);
                if (j != vertices.Length - 1)
                    roadLength += Vector3.Distance(vertices[j], vertices[j + 1]);
            }
            VertexPath vertexPath;
            // create new game object
            GameObject temp = new GameObject("Road");
            temp.transform.parent = parent;
            temp.transform.Rotate(new Vector3(0, 0, 0));
            if (vertices3D.Length > 1)
            {
                vertexPath = RoadCreator.GeneratePath(vertices3D, false, temp);
            }
            else
            {
                Debug.LogWarning("Road with 0 or 1 vertices found. Skipping: " + roadData.footprint.Count);
                return null;
            }


            //sUnity.Instantiate(temp, parent, position, rotation);
            //AddNodes(roadData, temp);

            if (vertexPath != null)
            {
                Mesh mesh = CreateRoadMesh(vertexPath);
                mesh.name = "road mesh";
                MeshFilter meshFilter = temp.AddComponent<MeshFilter>();
                meshFilter.sharedMesh = mesh;
                // temp.AddComponent<MeshCollider>().sharedMesh = mesh; // disabled mesh collider on roads
                // create duplicate of mat
                Material instanceOfRoadMat = new Material(roadShader);
                instanceOfRoadMat.SetFloat(NumberOfDashes, roadLength / 5);
                temp.AddComponent<MeshRenderer>().sharedMaterial = instanceOfRoadMat;
                //temp.GetComponent<PathCreator>().bezierPath = new BezierPath(vertices3D, false, PathSpace.xyz);
                temp.transform.position = new Vector3(roadData.center.x, 0, roadData.center.y);
                //meshFilter.mesh = mesh;
                temp.name = "Road";
                //snap to terrain
                mesh = temp.GetComponent<MeshFilter>().sharedMesh;
                Vector3[] GOvertices = mesh.vertices;
                for (int i = 0; i < GOvertices.Length; i++)
                {

                    // Vector3 prevPos = temp.transform.TransformPoint(GOvertices[i]);
                    // Vector3 nextPos = temp.transform.TransformPoint(GOvertices[i]);
                    // if(i > 0)
                    // {
                    //     prevPos = temp.transform.TransformPoint(GOvertices[i-1]);
                    // }

                    Vector3 worldPos = temp.transform.TransformPoint(GOvertices[i]);

                    if (Physics.Raycast(worldPos + Vector3.up * 1000, Vector3.down, out var hit, 10000))
                    {
                        Vector3 snapPoint = hit.point;
                        double height = elevation.SampleHeightFromPosition(worldPos);
                        if (hit.point.y > height + 5)
                        {
                            GOvertices[i].y = 0.01f + (float)height;
                        }
                        else
                        {
                            GOvertices[i].y = snapPoint.y + 0.2f + deltaHeight;
                        }
                    
                    }
                    else
                    {
                        Debug.Log("raycasts to snap roads missed");
                    }
                }
            
                mesh.vertices = GOvertices;
                mesh.RecalculateBounds();
            }
            else
            {
                Debug.LogError("Way shouldn't have a null vertex path. This should have been caught");
            }

            return temp;
        }

        public override void Release()
        {
            base.Release();
            networkGraph = null;
            roadsGameObjects = null;
        }
    }
}
