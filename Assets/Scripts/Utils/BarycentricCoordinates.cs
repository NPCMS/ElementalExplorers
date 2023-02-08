using UnityEngine;

namespace Utils
{
    public class BarycentricCoordinates
    {
        // computes the area of a traingle defined by 3 Vector3
        private static float AreaOf3dTri(Vector3 v0, Vector3 v1, Vector3 v2)
        {
            return Vector3.Magnitude(Vector3.Cross(v0 - v1, v2 - v0));
        }
        
        // returns a 3 length array of form [alpha, beta, gamma]
        public static float[] ComputeThreeDimensionalBarycentricCoords(Vector3[] tri, Vector3 point)
        {
            /* Calculate area of triangle ABC */
            float fullArea = AreaOf3dTri(tri[0], tri[1], tri[2]);
            /* Calculate area of triangle PBC */
            float pbc = AreaOf3dTri(point, tri[1], tri[2]);
            /* Calculate area of triangle PAC */
            float pac = AreaOf3dTri(tri[0], point, tri[2]);
            /* Calculate area of triangle PAB */
            float pab = AreaOf3dTri(tri[0], tri[1], point);
            float alpha = pbc / fullArea;
            float beta = pac / fullArea;
            float gamma = pab / fullArea;
            return new float[] {alpha, beta, gamma};
        }
    }
}