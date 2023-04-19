using System.Collections.Generic;
using UnityEngine;

public class WayToMesh
{
    private const float ScaleUV = 0.01f;
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

    public static Vector2[] MakeAntiClockwise(Vector2[] way)
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

    private static Vector2[] MakeClockwise(Vector2[] way)
    {
        float sum = 0;
        for (int i = 0; i < way.Length; i++)
        {
            Vector2 v0 = way[i];
            Vector2 v1 = way[ReMap(i + 1, way.Length)];
            sum += (v1.x - v0.x) * (v1.y + v0.y);
        }

        if (sum < 0)
        {
            var l = new List<Vector2>(way);
            l.Reverse();
            return l.ToArray();
        }

        return way;
    }


    public static bool TryCreateBuilding(OSMBuildingData building, out Mesh mesh, bool createRoof = false)
    {
        Vector2[] way = building.footprint.ToArray();
        way = MakeAntiClockwise(way);
        List<Vector3> verticies = new List<Vector3>();
        List<int> triangles = new List<int>();
        List<Vector2> uvs = new List<Vector2>();
        CreateWalls(building, way, verticies, triangles, uvs);
        bool success = true;
        if (createRoof)
        {
            try
            {
                success = CreateRoof(building, way, verticies, triangles, uvs);
            }
            catch (System.Exception)
            {
                success = false;
            }
        }
        mesh = new Mesh();
        mesh.vertices = verticies.ToArray();
        mesh.triangles = triangles.ToArray();
        mesh.uv = uvs.ToArray();
        mesh.RecalculateNormals();
        return success;
    }

    private static void CreateWalls(OSMBuildingData building, Vector2[] way, List<Vector3> verticies, List<int> triangles, List<Vector2> uvs)
    {
        float uvLength = 0;
        //create walls
        for (int i = 0; i < way.Length; i++)
        {
            int next = ReMap(i + 1, way.Length);
            int before = verticies.Count;
            verticies.Add(new Vector3(way[i].x, 0, way[i].y));
            verticies.Add(new Vector3(way[next].x, 0, way[next].y));
            float length = (verticies[^1] - verticies[^2]).magnitude;
            uvs.Add(new Vector2(uvLength, 0) * ScaleUV);
            uvs.Add(new Vector2(uvLength + length, 0) * ScaleUV);
            verticies.Add(new Vector3(way[i].x, building.buildingHeight, way[i].y));
            verticies.Add(new Vector3(way[next].x, building.buildingHeight, way[next].y));
            uvs.Add(new Vector2(uvLength, building.buildingHeight) * ScaleUV);
            uvs.Add(new Vector2(uvLength + length, building.buildingHeight) * ScaleUV);
            uvLength += length;
            triangles.Add(before);
            triangles.Add(before + 2);
            triangles.Add(before + 3);
            triangles.Add(before);
            triangles.Add(before + 3);
            triangles.Add(before + 1);
        }

        uvLength = 0;
        for (int i = 0; i < building.holes.Length; i++)
        {
            Vector2[] hole = MakeClockwise(building.holes[i]);
            for (int j = 0; j < building.holes[i].Length; j++)
            {
                int next = ReMap(j + 1, hole.Length);
                int before = verticies.Count;
                verticies.Add(new Vector3(hole[j].x, 0, hole[j].y));
                verticies.Add(new Vector3(hole[next].x, 0, hole[next].y));
                float length = (verticies[^1] - verticies[^2]).magnitude;
                uvs.Add(new Vector2(uvLength, 0) * ScaleUV);
                uvs.Add(new Vector2(uvLength + length, 0) * ScaleUV);
                verticies.Add(new Vector3(hole[j].x, building.buildingHeight, hole[j].y));
                verticies.Add(new Vector3(hole[next].x, building.buildingHeight, hole[next].y));
                uvs.Add(new Vector2(uvLength, building.buildingHeight) * ScaleUV);
                uvs.Add(new Vector2(uvLength + length, building.buildingHeight) * ScaleUV);
                triangles.Add(before);
                triangles.Add(before + 2);
                triangles.Add(before + 3);
                triangles.Add(before);
                triangles.Add(before + 3);
                triangles.Add(before + 1);
            }
        }
    }

    public static bool CreateFootprint(OSMBuildingData building, List<Vector3> verticies,
        List<int> triangles)
    {
        bool success = true;
        try
        {
            success = CreateRoof(building, MakeAntiClockwise(building.footprint.ToArray()), verticies, triangles, new List<Vector2>(), true);
        }
        catch (System.Exception)
        {
            success = false;
        }

        return success;
    }

    public static bool CreateRoofMesh(OSMBuildingData building, out Mesh mesh)
    {
        Vector2[] way = building.footprint.ToArray();
        way = MakeAntiClockwise(way);
        List<Vector3> verts = new List<Vector3>();
        List<int> tris = new List<int>();
        List<Vector2> uvs = new List<Vector2>();
        bool success = CreateRoof(building, way, verts, tris, uvs);
        mesh = new Mesh() {vertices = verts.ToArray(), triangles = tris.ToArray()};
        Vector3[] normals = new Vector3[verts.Count];
        for (int i = 0; i < normals.Length; i++)
        {
            normals[i] = Vector3.up;
        }
        mesh.normals = normals;
        mesh.uv = uvs.ToArray();
        return success;
    }

    private static bool CreateRoof(OSMBuildingData building, Vector2[] way, List<Vector3> verticies, List<int> triangles, List<Vector2> uvs, bool floor = false)
    {
        float height = floor ? 0 : building.buildingHeight;
        //create roof
        for (int i = 0; i < way.Length; i++)
        {
            verticies.Add(new Vector3(way[i].x, height, way[i].y));
            uvs.Add(way[i] * ScaleUV);
        }

        int holeVerticies = 0;
        Vector2[][] holes = new Vector2[building.holes.Length][];
        if (building.holes != null)
        {
            for (int i = 0; i < building.holes.Length; i++)
            {
                building.holes[i] = MakeClockwise(building.holes[i]);

                holeVerticies += building.holes[i].Length;
                for (int j = 0; j < building.holes[i].Length; j++)
                {
                    verticies.Add(new Vector3(building.holes[i][j].x, height, building.holes[i][j].y));
                    uvs.Add(building.holes[i][j] * ScaleUV);
                }
            }

            for (int i = 0; i < holes.Length; i++)
            {
                holes[i] = new Vector2[building.holes[i].Length];
                Vector2[] v = building.holes[i];
                for (int j = 0; j < v.Length; j++)
                {
                    holes[i][j] = v[j];
                }
            }
        }

        //triangulate using Sebastian Lauge's Triangulator
        var roofTriangles = new Sebastian.Geometry.Triangulator(new Sebastian.Geometry.Polygon(way, holes)).Triangulate();
        //triangulation is successful
        if (roofTriangles != null)
        {
            //old ear clipping implementation
            //List<int> roof = FillPolygon(verticies.GetRange(verticies.Count - way.Length, way.Length));
            for (int i = 0; i < roofTriangles.Length; i++)
            {
                roofTriangles[i] += verticies.Count - way.Length - holeVerticies;
            }

            triangles.AddRange(roofTriangles);
            return true;
        }
        else
        {
            //failed triangulation
            return false;
        }
    }
}