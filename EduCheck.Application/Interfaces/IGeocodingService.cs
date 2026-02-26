namespace EduCheck.Application.Interfaces;

public interface IGeocodingService
{
    /// <summary>
    /// Converts a physical address to latitude/longitude coordinates
    /// </summary>
    /// <param name="address">Physical address to geocode</param>
    /// <returns>Tuple of (Latitude, Longitude) or null if geocoding fails</returns>
    Task<(decimal? Latitude, decimal? Longitude)?> GeocodeAddressAsync(string address);

    /// <summary>
    /// Geocodes all institutes in the database that have addresses but no coordinates
    /// </summary>
    /// <returns>Number of institutes successfully geocoded</returns>
    Task<int> GeocodeAllInstitutesAsync();
}