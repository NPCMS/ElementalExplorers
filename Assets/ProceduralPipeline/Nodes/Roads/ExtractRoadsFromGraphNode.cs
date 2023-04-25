using System;
using System.Collections.Generic;
using System.Linq;
using QuikGraph;
using UnityEngine;
using XNode;
using RoadNetworkGraph = QuikGraph.UndirectedGraph<RoadNetworkNode, QuikGraph.TaggedEdge<RoadNetworkNode, RoadNetworkEdge>>;

[CreateNodeMenu("Roads/Extract Roads from Graph")]
public class ExtractRoadsFromGraphNode : AsyncExtendedNode
{
    [Input] public RoadNetworkGraph networkGraph;
    [Output] public List<OSMRoadsData> roads;
    protected override void CalculateOutputsAsync(Action<bool> callback)
    {
        RoadNetworkGraph roadsGraph = GetInputValue("networkGraph", networkGraph).Clone();
        roads = GetRoadsFromGraph(roadsGraph);
        callback.Invoke(true);
    }

    public override object GetValue(NodePort port)
    {
        if (port.fieldName == "roads") return roads;
        return null;
    }

    // gets a node list from a graph. This modifies the given graph and will remove all edges. Could be expensive so might be worth running on a different thread
    private static List<OSMRoadsData> GetRoadsFromGraph(RoadNetworkGraph roadsGraph)
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
                    if (edge.Tag.type.highway != path[0].Tag.type.highway) continue; // change in road type
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
                    if (edge.Tag.type.highway != path[^1].Tag.type.highway) continue; // change in road type
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
    
    protected override void ReleaseData()
    {
        networkGraph = null;
    }
}
