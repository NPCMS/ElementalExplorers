using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using QuikGraph;
using UnityEngine;
using XNode;

public class CalculatePOIRoute : SyncExtendedNode
{
    [Input] public List<GeoCoordinate> pointsOfInterest;
    [Output] public List<GeoCoordinate> raceRoute;

    public override object GetValue(NodePort port)
    {
        if (port.fieldName == "raceRoute") return raceRoute;
        return null;
    }

    public override IEnumerator CalculateOutputs(Action<bool> callback)
    {
        Debug.Log("We are here!!!!!");
        var pois = GetInputValue("pointsOfInterest", pointsOfInterest);

        var distanceGraph = new UndirectedGraph<GeoCoordinate, EquatableEdge<GeoCoordinate>>();

        foreach (GeoCoordinate poi in pois)
        {
            distanceGraph.AddVertex(poi);
        }

        Dictionary<EquatableEdge<GeoCoordinate>, double> poiDistances =
            new Dictionary<EquatableEdge<GeoCoordinate>, double>();

        for (int i = 0; i < pois.Count; i++)
        {
            for (int j = i + 1; j < pois.Count; j++)
            {
                var edge = new EquatableEdge<GeoCoordinate>(pois[i], pois[j]);
                poiDistances[edge] = GlobeBoundingBox.HaversineDistance(pois[i], pois[j]);
                distanceGraph.AddEdge(edge);
            }
        }

        raceRoute = TSPSolve(distanceGraph, poiDistances, pois[0]);

        callback.Invoke(true);
        yield break;
    }

    private List<GeoCoordinate> TSPSolve(UndirectedGraph<GeoCoordinate, EquatableEdge<GeoCoordinate>> g,
        Dictionary<EquatableEdge<GeoCoordinate>, double> weights, GeoCoordinate start)
    {
        // build spanning tree from g and store in newG
        UndirectedGraph<GeoCoordinate, EquatableEdge<GeoCoordinate>> newG =
            new UndirectedGraph<GeoCoordinate, EquatableEdge<GeoCoordinate>>();
        newG.AddVertexRange(g.Vertices);
        List<EquatableEdge<GeoCoordinate>> edgesToTest = new List<EquatableEdge<GeoCoordinate>>{g.Edges.First(_ => true)};
        HashSet<GeoCoordinate> visitedNodes = new HashSet<GeoCoordinate>{edgesToTest[0].Source};
        while (newG.EdgeCount != g.VertexCount - 1)
        {
            var minEdgeWeight = edgesToTest.Min(e => weights[e]);
            var minEdge = edgesToTest.First(e => Math.Abs(weights[e] - minEdgeWeight) < 0.00001);
            edgesToTest.Remove(minEdge);
            
            newG.AddEdge(minEdge);
            (_, bool duplicated) = DFS(newG, minEdge.Source);
            if (duplicated)
            {
                newG.RemoveEdge(minEdge);
            }
            else
            {
                GeoCoordinate newNode = visitedNodes.Contains(minEdge.Source) ? minEdge.Target : minEdge.Source;
                visitedNodes.Add(newNode);
                edgesToTest.AddRange(g.AdjacentEdges(newNode)
                    .Where(adjacentEdge => !(visitedNodes.Contains(adjacentEdge.Source) && visitedNodes.Contains(adjacentEdge.Target))));
            }
        }
        // get 2 approximation by visiting nodes is dfs order
        var (path, _) = DFS(newG, start);

        return path;
    }

    private (List<GeoCoordinate>, bool) DFS(UndirectedGraph<GeoCoordinate, EquatableEdge<GeoCoordinate>> g, GeoCoordinate start)
    {
        Stack<GeoCoordinate> nodes = new Stack<GeoCoordinate>();
        List<GeoCoordinate> visited = new List<GeoCoordinate>();
        nodes.Push(start);
        bool duplicate = false;

        while (nodes.Count > 0)
        {
            var next = nodes.Pop();
            
            if (visited.Contains(next))
            {
                duplicate = true;
                continue;
            }
            
            visited.Add(next);

            foreach (var adjacentVertex in g.AdjacentVertices(next))
            {
                if (!visited.Contains(adjacentVertex)) nodes.Push(adjacentVertex);
            }
        }
        
        return (visited, duplicate);
    }
    
    public override void Release()
    {
        // todo this should be removed later as it should be set by the previous nodes in the pipeline
        // pointsOfInterest = null;
    }
}
