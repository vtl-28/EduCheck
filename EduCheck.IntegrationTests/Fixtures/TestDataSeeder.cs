using EduCheck.Domain.Entities;

namespace EduCheck.IntegrationTests.Fixtures;

public static class TestDataSeeder
{
    /// <summary>
    /// Creates test institutes for integration testing
    /// </summary>
    public static List<Institute> CreateTestInstitutes(int count)
    {
        var institutes = new List<Institute>();
        var provinces = new[] { "Gauteng", "Western Cape", "KwaZulu-Natal", "Eastern Cape" };
        var cities = new[] { "Johannesburg", "Cape Town", "Durban", "Port Elizabeth" };
        var types = new[] { "Accredited", "Provisionally Accredited" };

        for (int i = 1; i <= count; i++)
        {
            var province = provinces[i % provinces.Length];
            var city = cities[i % cities.Length];
            var type = types[i % types.Length];

            institutes.Add(new Institute
            {
                Id = i,
                InstitutionName = $"Test Institute {i:D3}",
                AccreditationNumber = $"16 TEST {i:D5}",
                AccreditationPeriod = "01 January 2020",
                ProviderType = type,
                Province = province,
                City = city,
                PhysicalAddress = $"{i} Test Street, {city}, {province}",
                PostalAddress = $"P O Box {i}, {city}, {i:D4}",
                Telephone = $"011 555 {i:D4}",
                CreatedAt = DateTime.UtcNow.AddDays(-30),
                UpdatedAt = DateTime.UtcNow,
                IsActive = true
            });
        }

        return institutes;
    }

    /// <summary>
    /// Creates test users for authentication testing
    /// </summary>
    public static (string Email, string Password, string Role)[] CreateTestUsers()
    {
        return new[]
        {
            ("student1@test.com", "Test@123", "Student"),
            ("student2@test.com", "Test@123", "Student"),
            ("admin1@test.com", "Admin@123", "Admin"),
            ("admin2@test.com", "Admin@123", "Admin"),
            ("testuser@test.com", "Test@123", "Student")
        };
    }
}