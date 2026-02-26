using EduCheck.Application.Interfaces;
using EduCheck.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using System.Text.Json;

namespace EduCheck.Infrastructure.Services;

public class GeocodingService : IGeocodingService
{
    private readonly ApplicationDbContext _context;
    private readonly IConfiguration _configuration;
    private readonly ILogger<GeocodingService> _logger;
    private readonly HttpClient _httpClient;
    private readonly string _googleApiKey;

    public GeocodingService(
        ApplicationDbContext context,
        IConfiguration configuration,
        ILogger<GeocodingService> logger,
        HttpClient httpClient)
    {
        _context = context;
        _configuration = configuration;
        _logger = logger;
        _httpClient = httpClient;
        _googleApiKey = _configuration["GoogleMaps:ApiKey"]
            ?? throw new InvalidOperationException("Google Maps API key not configured");
    }

    public async Task<(decimal? Latitude, decimal? Longitude)?> GeocodeAddressAsync(string address)
    {
        if (string.IsNullOrWhiteSpace(address))
        {
            _logger.LogWarning("Cannot geocode empty address");
            return null;
        }

        try
        {
            // Build Google Geocoding API URL
            var encodedAddress = Uri.EscapeDataString(address);
            var url = $"https://maps.googleapis.com/maps/api/geocode/json?address={encodedAddress}&key={_googleApiKey}";

            // Make HTTP request
            var response = await _httpClient.GetAsync(url);
            response.EnsureSuccessStatusCode();

            // Parse JSON response
            var json = await response.Content.ReadAsStringAsync();
            var geocodeResponse = JsonSerializer.Deserialize<GeocodeResponse>(json, new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            });

            if (geocodeResponse?.Status == "OK" && geocodeResponse.Results?.Any() == true)
            {
                var location = geocodeResponse.Results.First().Geometry.Location;
                var lat = location.Lat;
                var lng = location.Lng;

                _logger.LogInformation("Successfully geocoded address: {Address} -> ({Lat}, {Lng})",
                    address, lat, lng);

                return (lat, lng);
            }

            _logger.LogWarning("Geocoding failed for address: {Address}. Status: {Status}",
                address, geocodeResponse?.Status ?? "Unknown");
            return null;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error geocoding address: {Address}", address);
            return null;
        }
    }

    public async Task<int> GeocodeAllInstitutesAsync()
    {
        _logger.LogInformation("Starting geocoding process for all institutes");

        // Get institutes that have addresses but no coordinates
        var institutesToGeocode = await _context.Institutes
            .Where(i => (i.Latitude == null || i.Longitude == null)
                     && !string.IsNullOrEmpty(i.PhysicalAddress))
            .ToListAsync();

        _logger.LogInformation("Found {Count} institutes to geocode", institutesToGeocode.Count);

        if (!institutesToGeocode.Any())
        {
            _logger.LogInformation("No institutes need geocoding");
            return 0;
        }

        var successCount = 0;
        var failedAddresses = new List<string>();

        for (var i = 0; i < institutesToGeocode.Count; i++)
        {
            var institute = institutesToGeocode[i];

            _logger.LogInformation("Geocoding {Current}/{Total}: {Name}",
                i + 1, institutesToGeocode.Count, institute.InstitutionName);

            // Build full address for better geocoding accuracy
            var fullAddress = BuildFullAddress(institute);
            var coordinates = await GeocodeAddressAsync(fullAddress);

            if (coordinates.HasValue)
            {
                institute.Latitude = coordinates.Value.Latitude;
                institute.Longitude = coordinates.Value.Longitude;
                successCount++;

                _logger.LogInformation("✓ Geocoded: {Name} -> ({Lat}, {Lng})",
                    institute.InstitutionName,
                    coordinates.Value.Latitude,
                    coordinates.Value.Longitude);
            }
            else
            {
                failedAddresses.Add($"{institute.InstitutionName}: {fullAddress}");
                _logger.LogWarning("✗ Failed to geocode: {Name}", institute.InstitutionName);
            }

            // Save after each geocode to avoid losing progress
            await _context.SaveChangesAsync();

            // Rate limiting: Google allows 50 requests/second
            // Being conservative with 20 requests/second (50ms delay)
            if (i < institutesToGeocode.Count - 1)
            {
                await Task.Delay(50);
            }
        }

        _logger.LogInformation("Geocoding complete. Success: {Success}/{Total}",
            successCount, institutesToGeocode.Count);

        if (failedAddresses.Any())
        {
            _logger.LogWarning("Failed to geocode {Count} addresses:", failedAddresses.Count);
            foreach (var failed in failedAddresses)
            {
                _logger.LogWarning("  - {Address}", failed);
            }
        }

        return successCount;
    }

    private string BuildFullAddress(Domain.Entities.Institute institute)
    {
        // Build most complete address possible for better geocoding accuracy
        var addressParts = new List<string>();

        if (!string.IsNullOrWhiteSpace(institute.PhysicalAddress))
            addressParts.Add(institute.PhysicalAddress);

        if (!string.IsNullOrWhiteSpace(institute.City))
            addressParts.Add(institute.City);

        if (!string.IsNullOrWhiteSpace(institute.Province))
            addressParts.Add(institute.Province);

        // Always add South Africa for better accuracy
        addressParts.Add("South Africa");

        return string.Join(", ", addressParts);
    }

    // JSON response models for Google Geocoding API
    private class GeocodeResponse
    {
        public string Status { get; set; } = string.Empty;
        public List<GeocodeResult> Results { get; set; } = new();
    }

    private class GeocodeResult
    {
        public Geometry Geometry { get; set; } = new();
    }

    private class Geometry
    {
        public Location Location { get; set; } = new();
    }

    private class Location
    {
        public decimal? Lat { get; set; }
        public decimal? Lng { get; set; }
    }
}