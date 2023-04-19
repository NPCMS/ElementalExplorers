using Unity.Mathematics;
using UnityEngine;

[System.Serializable]
public class ElevationData
{
    public float[,] height;
    public GlobeBoundingBox box;
    public double maxHeight;
    public double minHeight;

    public ElevationData(float[,] height, GlobeBoundingBox box, double minHeight, double maxHeight)
    {
        this.height = height;
        this.box = box;
        this.maxHeight = maxHeight;
        this.minHeight = minHeight;
    }
    
    //calculate pixels to interpolate for bilinear filtering in heightmap image
    private void GetInterpolation(float x, double terrainWidth, int width, out int from, out int to, out double t)
    {
        //proportion of the image to sample from
        double it = x / terrainWidth;
        //the pixel this point is inside of
        int pixelContained = (int)(it * width);
        //proportion of the pixel
        t = (it - (double)pixelContained / width) * width;

        //assume closer to next pixel
        from = pixelContained;
        to = pixelContained + 1;

        //closer to previous pixel
        if (t < 0.5f)
        {
            t += 0.5f;
            from--;
            //first pixel
            if (from < 0)
            {
                from = 0;
            }
            else
            {
                to--;
            }
        }
        //closer to next pixel
        else
        {
            t -= 0.5f;
            if (to >= width)
            {
                to = width - 1;
            }
        }
    }

    private double DoubleLerp(double a, double b, double t)
    {
        return a * (1 - t) + b * t;
    }

    public double SampleHeightFromPosition(Vector3 position)
    {
        double width = GlobeBoundingBox.LatitudeToMeters(box.north - box.south);
        int heightResolution = height.GetLength(0);
        position.x = Mathf.Clamp(position.x, 0, (float)width - 1);
        position.z = Mathf.Clamp(position.z, 0, (float)width - 1);
        GetInterpolation(position.z, width, heightResolution, out int xFrom, out int xTo, out double xT);
        GetInterpolation(position.x, width, heightResolution, out int yFrom, out int yTo, out double yT);

        xFrom = Mathf.Clamp(xFrom, 0, height.GetLength(0) - 1);
        xTo = Mathf.Clamp(xFrom, 0, height.GetLength(0) - 1);
        yFrom = Mathf.Clamp(yFrom, 0, height.GetLength(1) - 1);
        yTo = Mathf.Clamp(yFrom, 0, height.GetLength(1) - 1);
        
        double yFromLerp = DoubleLerp(height[xFrom, yFrom], height[xFrom, yTo], yT);
        double yToLerp = DoubleLerp(height[xTo, yFrom], height[xTo, yTo], yT);
        return (minHeight + DoubleLerp(yFromLerp, yToLerp, xT) * (maxHeight - minHeight));
    }
    
    public double SampleHeightFromPositionAccurate(Vector3 position)
    {
        float width = (float)GlobeBoundingBox.LatitudeToMeters(box.north - box.south);
        int heightResolution = height.GetLength(0);
        float2 uv = new float2(position.z, position.x) / width;
        uv = math.clamp(uv, new float2(0, 0), new float2(1, 1));
        float2 samplePos = uv * (heightResolution - 1);
        float2 sampleFloor = math.floor(samplePos);
        float2 sampleDecimal = samplePos - sampleFloor;
        int upperLeftTri = sampleDecimal.y > sampleDecimal.x ? 1 : 0;

        float3 v0 = GetVertexLocalPos((int)sampleFloor.x, (int)sampleFloor.y, heightResolution, width);
        float3 v1 = GetVertexLocalPos(math.min((int)sampleFloor.x + 1, heightResolution - 1), math.min((int)sampleFloor.y + 1, heightResolution - 1), heightResolution, width);

        float3 v2 = GetVertexLocalPos(math.min((int)sampleFloor.x + 1 - upperLeftTri, heightResolution - 1), math.min((int)sampleFloor.y + upperLeftTri, heightResolution - 1), heightResolution, width);
        float3 n = math.cross(v1 - v0, v2 - v0);
        return minHeight + (maxHeight - minHeight) *
            (((-n.x * (position.z - v0.x) - n.z * (position.x - v0.z)) / n.y) + v0.y);
    }

    float3 GetVertexLocalPos(int x, int y, int heightMapResolution, float width)
    {
        float heightValue = height[x,y];
        return new float3(
            (float)x / (heightMapResolution - 1) * width,
            heightValue,
            (float)y / (heightMapResolution - 1) * width);
    }
}
