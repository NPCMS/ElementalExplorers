using System.Collections.Generic;
using UnityEngine;

namespace Utils
{
    public class SampleRandomPointsOnTri
    {
        // https://dev.to/bogdanalexandru/generating-random-points-within-a-polygon-in-unity-nce
        public static List<Vector3> SampleRandPointsOnTri(int numPoints, Vector3[] tri)
        {
            List<Vector3> points = new List<Vector3>();
            for (int i = 0; i < numPoints; i++)
            {
                // pre cache random numbers and the sqrt for performance
                float randOne = Random.Range(0.0f, 1.0f);
                float sqrtRandOne = Mathf.Sqrt(randOne);
                float randTwo = Random.Range(0.0f, 1.0f);
                
                // get random point on tri
                Vector3 newPoint = ((1 - sqrtRandOne) * tri[0]) +
                                   ((sqrtRandOne * (1 - randTwo)) * tri[1]) +
                                   ((randTwo * sqrtRandOne) * tri[2]);
                points.Add(newPoint);
            }
            return points;
        }

        public static List<Vector3> SampleRandPointsOnTriAsync(int numPoints, Vector3[] tri, System.Random rnd)
        {
            List<Vector3> points = new List<Vector3>();
            for (int i = 0; i < numPoints; i++)
            {
                // pre cache random numbers and the sqrt for performance
                float randOne = (float)rnd.NextDouble();
                float sqrtRandOne = Mathf.Sqrt(randOne);
                float randTwo = (float)rnd.NextDouble();

                // get random point on tri
                Vector3 newPoint = ((1 - sqrtRandOne) * tri[0]) +
                                   ((sqrtRandOne * (1 - randTwo)) * tri[1]) +
                                   ((randTwo * sqrtRandOne) * tri[2]);
                points.Add(newPoint);
            }
            return points;
        }
    }
}