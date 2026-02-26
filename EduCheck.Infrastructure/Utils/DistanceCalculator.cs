namespace EduCheck.Infrastructure.Utils;

/// <summary>
/// Utility class for calculating distances between geographic coordinates
/// Uses Haversine formula for accurate distance calculation on a sphere (Earth)
/// </summary>
public static class DistanceCalculator
{
    private const decimal EarthRadiusKm = 6371m; // Earth's radius in kilometers

    /// <summary>
    /// Calculate distance between two points on Earth using Haversine formula
    /// </summary>
    /// <param name="lat1">Latitude of first point in decimal degrees</param>
    /// <param name="lng1">Longitude of first point in decimal degrees</param>
    /// <param name="lat2">Latitude of second point in decimal degrees</param>
    /// <param name="lng2">Longitude of second point in decimal degrees</param>
    /// <returns>Distance in kilometers</returns>
    /// <remarks>
    /// Haversine formula:
    /// a = sin²(Δlat/2) + cos(lat1) × cos(lat2) × sin²(Δlng/2)
    /// c = 2 × atan2(√a, √(1−a))
    /// distance = R × c
    /// 
    /// Where R is Earth's radius (6371 km)
    /// </remarks>
    public static decimal CalculateDistance(decimal lat1, decimal lng1, decimal lat2, decimal lng2)
    {
        // Convert degrees to radians
        var lat1Rad = ToRadians(lat1);
        var lng1Rad = ToRadians(lng1);
        var lat2Rad = ToRadians(lat2);
        var lng2Rad = ToRadians(lng2);

        // Calculate differences
        var dLat = lat2Rad - lat1Rad;
        var dLng = lng2Rad - lng1Rad;

        // Haversine formula
        var a = (decimal)(
            Math.Pow((double)Math.Sin((double)dLat / 2), 2) +
            Math.Cos((double)lat1Rad) * Math.Cos((double)lat2Rad) *
            Math.Pow((double)Math.Sin((double)dLng / 2), 2)
        );

        var c = (decimal)(2 * Math.Atan2(Math.Sqrt((double)a), Math.Sqrt(1 - (double)a)));

        var distance = EarthRadiusKm * c;

        return distance;
    }

    /// <summary>
    /// Convert degrees to radians
    /// </summary>
    private static decimal ToRadians(decimal degrees)
    {
        return degrees * (decimal)Math.PI / 180m;
    }

    /// <summary>
    /// Validate latitude is within valid range (-90 to +90)
    /// </summary>
    public static bool IsValidLatitude(decimal latitude)
    {
        return latitude >= -90m && latitude <= 90m;
    }

    /// <summary>
    /// Validate longitude is within valid range (-180 to +180)
    /// </summary>
    public static bool IsValidLongitude(decimal longitude)
    {
        return longitude >= -180m && longitude <= 180m;
    }
}