using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Unity.Jobs;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.UIElements;

public class WayToMesh
{

    //https://www.geometrictools.com/Documentation/TriangulationByEarClipping.pdf
    private static bool PointInTriangle(Vector3 point, Vector3 v0, Vector3 v1, Vector3 v2)
    {
        Vector3 e1 = v1 - v0;
        float d1 = e1.magnitude;
        e1 /= d1;
        Vector3 e2 = v2 - v0;
        float d2 = e2.magnitude;
        e2 /= d2;
        Vector3 dir = point - v0;
        float u = Vector3.Dot(dir, e1) / d1;
        float v = Vector3.Dot(dir, e2) / d2;

        return u >= 0 && u <= 1 && v >= 0 && v <= 1 && u + v <= 1;
    }

    private static bool IsEar(List<Vector3> verts, List<int> indexes, int v0, int v1, int v2)
    {
        for (int i = 0; i < indexes.Count; i++)
        {
            if (i != v0 && i != v1 && i != v2)
            {
                if (PointInTriangle(verts[indexes[i]], verts[indexes[v0]], verts[indexes[v1]], verts[indexes[v2]]))
                {
                    return false;
                }
            }
        }
        return true;
    }

    private static int ReMap(int i, int length)
    {
        return i < 0 ? length + i : (i >= length ? i - length : i);
    }

    private static bool IsConcave(Vector3 v0, Vector3 v1, Vector3 v2)
    {
        Vector3 e1 = Vector3.Cross(v1 - v0, Vector3.up);
        Vector3 e2 = v2 - v1;
        return Vector3.Dot(e1, e2) > 0;
    }

    private static void GetVertexLists(List<Vector3> verts, List<int> indexes, List<int> convex, List<int> reflex, List<int> ears)
    {
        convex.Clear();
        reflex.Clear();
        ears.Clear();
        for (int i = 0; i < indexes.Count; i++)
        {
            int previousIndex = ReMap(i - 1, indexes.Count);
            int nextIndex = ReMap(i + 1, indexes.Count);

            if (IsConcave(verts[indexes[previousIndex]], verts[indexes[i]], verts[indexes[nextIndex]]))
            {
                convex.Add(i);
                if (IsEar(verts, indexes, i, previousIndex, nextIndex))
                {
                    ears.Add(i);
                }
            }
            else
            {
                reflex.Add(i);
            }
        }
    }


    private static List<int> FillPolygon(List<Vector3> verts)
    {
        List<int> triangles = new List<int>();
        List<int> indexes = new List<int>();
        for (int i = 0; i < verts.Count; i++)
        {
            indexes.Add(i);
        }
        List<int> convex = new List<int>();
        List<int> reflex = new List<int>();
        List<int> ears = new List<int>();
        GetVertexLists(verts, indexes, convex, reflex, ears);

        while (ears.Count > 0)
        {
            int i0 = ears[0];
            int i1 = ReMap(i0 - 1, indexes.Count);
            int i2 = ReMap(i0 + 1, indexes.Count);
            triangles.Add(indexes[i0]);
            triangles.Add(indexes[i1]);
            triangles.Add(indexes[i2]);

            indexes.RemoveAt(i0);
            GetVertexLists(verts, indexes, convex, reflex, ears);
        }

        return triangles;
    }

    private static Vector2[] MakeAntiClockwise(Vector2[] way)
    {
        float sum = 0;
        for (int i = 0; i < way.Length; i++)
        {
            Vector2 v0 = way[i];
            Vector2 v1 = way[ReMap(i + 1, way.Length)];
            sum += (v1.x - v0.x) * (v1.y + v0.y);
        }

        if (sum > 0)
        {
            var l = new List<Vector2>(way);
            l.Reverse();
            return l.ToArray();
        }

        return way;
    }

    public static Mesh CreateBuilding(Vector2[] way, float height)
    {
        way = MakeAntiClockwise(way);
        List<Vector3> verticies = new List<Vector3>();
        List<int> triangles = new List<int>();

        for (int i = 0; i < way.Length; i++)
        {
            int next = ReMap(i + 1, way.Length);
            int before = verticies.Count;
            verticies.Add(new Vector3(way[i].x, 0, way[i].y));
            verticies.Add(new Vector3(way[next].x, 0, way[next].y));
            verticies.Add(new Vector3(way[i].x, height, way[i].y));
            verticies.Add(new Vector3(way[next].x, height, way[next].y));
            triangles.Add(before);
            triangles.Add(before + 2);
            triangles.Add(before + 3);
            triangles.Add(before);
            triangles.Add(before + 3);
            triangles.Add(before + 1);
        }

        for (int i = 0; i < way.Length; i++)
        {
            verticies.Add(new Vector3(way[i].x, height, way[i].y));
        }

        List<int> roof = FillPolygon(verticies.GetRange(verticies.Count - way.Length, way.Length));
        for (int i = 0; i < roof.Count; i++)
        {
            roof[i] += verticies.Count - way.Length;
        }

        triangles.AddRange(roof);
        Mesh mesh = new Mesh();
        mesh.vertices = verticies.ToArray();
        mesh.triangles = triangles.ToArray();
        mesh.RecalculateNormals();
        return mesh;
    }
}