using System;

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
}