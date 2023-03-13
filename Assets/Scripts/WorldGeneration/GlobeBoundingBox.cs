using System;
using UnityEngine;

[System.Serializable]
public struct GlobeBoundingBox
{
    private const double EarthRadius = 6378137;
    //defines the bounds
    public double north, east, south, west;

    public GlobeBoundingBox(double north, double east, double south, double west)
    {
        this.north = north;
        this.east = east;
        this.south = south;
        this.west = west;
    }

    public GlobeBoundingBox(double south, double west, double width)
    {
        this.south = south;
        this.west = west;
        this.east = AddMetersToLongitude(west, south, width);
        this.north = AddMetersToLatitude(south, width);
    }

    public Vector2 ConvertGeoCoordToMeters(Vector2 coord)
    {
        double width = LatitudeToMeters(north - south);
        if (width < 0.0001) Debug.LogError("Bounding box has no width");
        float verticalDst = Mathf.InverseLerp((float)south, (float)north, coord.x) * (float)width;
        float horizontalDst = Mathf.InverseLerp((float)west, (float)east, coord.y) * (float)width;
        return new Vector2(horizontalDst, verticalDst);
    }

    //converts change of latitude to meters 
    public static double LatitudeToMeters(double deltaLatitude)
    {
        return 111320 * deltaLatitude;
    }

    //converts change in latitude to meters
    public static double MetersToDegrees(double meters)
    {
        return meters / 111320;
    }

    //https://stackoverflow.com/questions/7477003/calculating-new-longitude-latitude-from-old-n-meters
    //ouputs the correct longitude after moving given meters east at a given latitude
    public static double AddMetersToLongitude(double longitude, double latitude, double meters)
    {
        return longitude + (meters / EarthRadius) * (180 / Math.PI) / Math.Cos(latitude * Math.PI / 180);
    }

    //outputs the correct latitude after moving given meters north
    public static double AddMetersToLatitude(double latitude, double meters)
    {
        return latitude + (meters / EarthRadius) * (180 / Math.PI);
    }
    
    //outputs the correct distance between two sets of geo coordinates
    public static double HaversineDistance(GeoCoordinate pos1, GeoCoordinate pos2)
    {
        const double r = 6378100; // meters
        
        var sdlat = Math.Sin(Radians(pos2.Latitude - pos1.Latitude) / 2);
        var sdlon = Math.Sin(Radians(pos2.Longitude - pos1.Longitude) / 2);
        var q = sdlat * sdlat + Math.Cos(Radians(pos1.Latitude)) * Math.Cos(Radians(pos2.Latitude)) * sdlon * sdlon;
        var d = 2 * r * Math.Asin(Math.Sqrt(q));
        return d;
    }
    
    public static double HaversineDistance(Vector2 pos1, Vector2 pos2)
    {
        const double r = 6378100; // meters
        
        var sdlat = Math.Sin(Radians(pos2.x - pos1.x) / 2);
        var sdlon = Math.Sin(Radians(pos2.y - pos1.y) / 2);
        var q = sdlat * sdlat + Math.Cos(Radians(pos1.x)) * Math.Cos(Radians(pos2.x)) * sdlon * sdlon;
        var d = 2 * r * Math.Asin(Math.Sqrt(q));
        return d;
    }

    private static double Radians(double degrees)
    {
        return degrees * Math.PI / 180;
    }
}