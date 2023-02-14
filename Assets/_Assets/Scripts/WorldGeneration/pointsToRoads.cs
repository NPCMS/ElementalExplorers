using UnityEngine;
using PathCreation;



public static class RoadCreator {

    public static VertexPath GeneratePath(Vector3[] points, bool closedPath, GameObject gameObj)
   {        
       // Create a closed, 2D bezier path from the supplied points array
       // These points are treated as anchors, which the path will pass through
       // The control points for the path will be generated automatically
       BezierPath bezierPath = new BezierPath(points, closedPath, PathSpace.xy);
       // Then create a vertex path from the bezier path, to be used for movement etc

       Transform newTransform = gameObj.transform;
       VertexPath vertexPath = new VertexPath(bezierPath, new GameObject().transform, 1);
       return vertexPath;
   }
}




