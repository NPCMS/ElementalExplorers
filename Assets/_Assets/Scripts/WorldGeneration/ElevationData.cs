using System;
using System.Collections;
using System.Collections.Generic;
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
        GetInterpolation(position.z, width, heightResolution, out int xFrom, out int xTo, out double xT);
        GetInterpolation(position.x, width, heightResolution, out int yFrom, out int yTo, out double yT);
        
        double yFromLerp = DoubleLerp(height[xFrom, yFrom], height[xFrom, yTo], yT);
        double yToLerp = DoubleLerp(height[xTo, yFrom], height[xTo, yTo], yT);
        return (minHeight + DoubleLerp(yFromLerp, yToLerp, xT) * (maxHeight - minHeight));
    }
    
}
