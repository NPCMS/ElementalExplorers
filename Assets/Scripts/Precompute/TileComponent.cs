using UnityEngine;
using static UnityEngine.UI.GridLayoutGroup;

public class TileComponent : MonoBehaviour
{
    private TerrainData terrainData;
    private Terrain terrain;
    public ElevationData ElevationData { get; private set; }
    public Texture2D GrassMask { get; private set; }

    private float height;
    private float offset;
    
    //Applies elevation to terrain
    public void SetTerrainElevation(ElevationData elevation, float width)
    {
        height = (float)(elevation.maxHeight - elevation.minHeight);
        offset = (float)elevation.minHeight;
        this.ElevationData = elevation;
        terrainData = new TerrainData();
        Debug.Assert(elevation.height.GetLength(0) == elevation.height.GetLength(1), "Heightmap is not square, run through upsample node before output");
        terrainData.heightmapResolution = elevation.height.GetLength(0);
        //double width = GlobeBoundingBox.LatitudeToMeters(elevation.box.north - elevation.box.south);
        terrainData.size = new Vector3(width, (float)(elevation.maxHeight - elevation.minHeight), width);
        terrainData.SetHeights(0, 0, elevation.height);
        GameObject go = Terrain.CreateTerrainGameObject(terrainData);
        go.transform.SetParent(transform, true);
        go.layer = 0;
        terrain = go.GetComponent<Terrain>();
        transform.position = new Vector3(0, (float)elevation.minHeight, 0);
    }

    public void SetGrassData(Texture2D mask)
    {
        GrassMask = mask;
    }

    public float GetTerrainWidth()
    {
        return terrainData.size.x;
    }

    public void SetTerrainOffset(Vector2 offset)
    {
        transform.position += new Vector3(offset.x, 0, -offset.y);
        StaticBatchingUtility.Combine(gameObject);
    }

    public void SetNeighbours(TileComponent bottom, TileComponent top, TileComponent left, TileComponent right, GameObject colliderPrefab)
    {
        bool l = left != null;
        bool b = bottom != null;
        Vector3 center = terrain.GetPosition() + Vector3.one * GetTerrainWidth() / 2.0f;
        center.y = 0;
        if (l)
        {
            StitchToLeft(left);
        }
        else
        {
            GameObject collider = Instantiate(colliderPrefab);
            collider.transform.position = center + Vector3.left * GetTerrainWidth() / 2.0f;
            collider.transform.eulerAngles = new Vector3(0, -90, 0);
            collider.transform.localScale = Vector3.one * GetTerrainWidth() + Vector3.up * 500.0f;
        }
        if (b)
        {
            StitchToBottom(bottom);
        }
        else
        {
            GameObject collider = Instantiate(colliderPrefab);
            collider.transform.position = center + Vector3.back * GetTerrainWidth() / 2.0f;
            collider.transform.eulerAngles = new Vector3(0, -180, 0);
            collider.transform.localScale = Vector3.one * GetTerrainWidth() + Vector3.up * 500.0f;
        }

        if (top == null)
        {
            GameObject collider = Instantiate(colliderPrefab);
            collider.transform.position = center + Vector3.forward * GetTerrainWidth() / 2.0f;
            collider.transform.localScale = Vector3.one * GetTerrainWidth() + Vector3.up * 500.0f;
        }

        if (right == null)
        {
            GameObject collider = Instantiate(colliderPrefab);
            collider.transform.position = center + Vector3.right * GetTerrainWidth() / 2.0f;
            collider.transform.eulerAngles = new Vector3(0, 90, 0);
            collider.transform.localScale = Vector3.one * GetTerrainWidth() + Vector3.up * 500.0f;
        }

    }

    public void SetCornerNeighbours(TileComponent bottom, TileComponent top, TileComponent left, TileComponent right, TileComponent corner)
    {
        bool l = left != null;
        bool b = bottom != null;
        if (l && b && corner != null)
        {
            StitchCorners(left, bottom, corner);
        }
        terrain.SetNeighbors(l ? left.terrain : null, top != null ? top.terrain : null, right != null ? right.terrain : null, b ? bottom.terrain : null);
    }

    private void StitchCorners(TileComponent leftNeighbor, TileComponent bottomNeighbor, TileComponent corner)
    {
        int resolution = terrainData.heightmapResolution;

        // Take the last x-column of neighbors heightmap array
        // 1 pixel wide (single x value), resolution pixels tall (all y values)
        float[,] thisEdge = terrainData.GetHeights(0, 0, 1, 1);
        float[,] leftEdge = leftNeighbor.terrainData.GetHeights(resolution - 1, 0, 1, 1);
        float[,] bottomEdge = bottomNeighbor.terrainData.GetHeights(0, resolution - 1, 1, 1);
        float[,] cornerEdge = corner.terrainData.GetHeights(resolution - 1, resolution - 1, 1, 1);
        float leftWorldHeight = leftEdge[0,0] * (leftNeighbor.height) + leftNeighbor.offset;
        float thisWorldHeight = thisEdge[0,0] * this.height + offset;
        float bottomHeight = bottomEdge[0, 0] * bottomNeighbor.height + bottomNeighbor.offset;
        float cornerHeight = cornerEdge[0, 0] * corner.height + corner.offset;
        float height = Mathf.Max(leftWorldHeight, thisWorldHeight, bottomHeight, cornerHeight);
        thisEdge[0, 0] = (height - offset) / this.height;
        leftEdge[0, 0] = (height - leftNeighbor.offset) / leftNeighbor.height;
        bottomEdge[0, 0] = (height - bottomNeighbor.offset) / bottomNeighbor.height;
        cornerEdge[0, 0] = (height - corner.offset) / corner.height;
        // Stitch with other terrain by setting same heightmap values on the edge
        terrainData.SetHeights(0, 0, thisEdge);
        bottomNeighbor.terrainData.SetHeights(0, resolution - 1, bottomEdge);
        leftNeighbor.terrainData.SetHeights(resolution - 1, 0, leftEdge);
        corner.terrainData.SetHeights(resolution - 1, resolution - 1, cornerEdge);
    }

    //https://gamedev.stackexchange.com/questions/175457/unity-seamless-terrain
    private void StitchToLeft(TileComponent leftNeighbor)
    {
        int resolution = terrainData.heightmapResolution;

        // Take the last x-column of neighbors heightmap array
        // 1 pixel wide (single x value), resolution pixels tall (all y values)
        float[,] edgeValues = leftNeighbor.terrainData.GetHeights(resolution - 1, 0, 1, resolution);
        float[,] thisEdgeValues = terrainData.GetHeights(0, 0, 1, resolution);
        for (int i = 0; i < edgeValues.GetLength(0); i++)
        {
            for (int j = 0; j < edgeValues.GetLength(1); j++)
            { 
                float worldHeight = edgeValues[i, j] * (leftNeighbor.height) + leftNeighbor.offset;
                float thisWorldHeight = thisEdgeValues[i, j] * this.height + offset;
                float height = Mathf.Min(worldHeight, thisWorldHeight);
                thisEdgeValues[i, j] = (height - offset) / this.height;
                edgeValues[i, j] = (height - leftNeighbor.offset) / leftNeighbor.height;
            }
        }

        // Stitch with other terrain by setting same heightmap values on the edge
        terrainData.SetHeights(0, 0, thisEdgeValues);
        leftNeighbor.terrainData.SetHeights(resolution - 1, 0, edgeValues);
    }

    //https://gamedev.stackexchange.com/questions/175457/unity-seamless-terrain
    private void StitchToBottom(TileComponent bottomNeighbor)
    {
        int resolution = terrainData.heightmapResolution;

        // Take the top y-column of neighbors heightmap array
        // resolution pixels wide (all x values), 1 pixel tall (single y value)
        float[,] edgeValues = bottomNeighbor.terrainData.GetHeights(0, resolution - 1, resolution, 1);
        float[,] thisEdgeValues = terrainData.GetHeights(0, 0, resolution, 1);
        for (int i = 0; i < edgeValues.GetLength(0); i++)
        {
            for (int j = 0; j < edgeValues.GetLength(1); j++)
            {
                float worldHeight = edgeValues[i, j] * (bottomNeighbor.height) + bottomNeighbor.offset;
                float thisWorldHeight = thisEdgeValues[i, j] * this.height + offset;
                float height = Mathf.Min(worldHeight, thisWorldHeight);
                thisEdgeValues[i, j] = (height - offset) / this.height;
                edgeValues[i, j] = (height - bottomNeighbor.offset) / bottomNeighbor.height;
            }
        }

        // Stitch with other terrain by setting same heightmap values on the edge
        terrainData.SetHeights(0, 0, thisEdgeValues);
        bottomNeighbor.terrainData.SetHeights(0, resolution - 1, edgeValues);
    }

    public void SetMaterial(Material terrainMaterial, Texture2D waterMask)
    {
        Material mat = new Material(terrainMaterial);
        mat.SetTexture("_WaterMask", waterMask);
        terrain.materialTemplate = mat;
    }

    public Texture2D GenerateHeightmap(out double minHeight, out double scale)
    {
        float[,] heights = terrainData.GetHeights(0, 0, terrainData.heightmapResolution, terrainData.heightmapResolution);
        int width = heights.GetLength(0);
        Texture2D height = new Texture2D(width, width, TextureFormat.RFloat, false, false);
        for (int i = 0; i < width; i++)
        {
            for (int j = 0; j < width; j++)
            {
                height.SetPixel(i, j, new Color(heights[j, i], 0, 0));
            }
        }

        height.Apply();
        minHeight = offset;
        scale = terrainData.heightmapScale.y;
        return height;
    }
}
