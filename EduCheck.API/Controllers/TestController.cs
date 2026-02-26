using Microsoft.AspNetCore.Mvc;
using EduCheck.API.Metrics;

namespace EduCheck.API.Controllers;

[ApiController]
[Route("api/[controller]")]
public class TestController : ControllerBase
{
    private readonly ILogger<TestController> _logger;
    private readonly BusinessMetrics _businessMetrics;

    public TestController(
        ILogger<TestController> logger,
        BusinessMetrics businessMetrics)
    {
        _logger = logger;
        _businessMetrics = businessMetrics;
    }

    [HttpGet("ping")]
    public IActionResult Ping()
    {
        _logger.LogInformation("Test endpoint called");


        _businessMetrics.RecordInstituteSearch(found: true, searchType: "test");

        return Ok(new { message = "pong", timestamp = DateTime.UtcNow });
    }

    [HttpGet("business-test")]
    public IActionResult BusinessTest()
    {

        _businessMetrics.RecordInstituteSearch(found: true, searchType: "name");
        _businessMetrics.RecordSearchDuration(150.5, found: true);
        _businessMetrics.RecordLoginAttempt(success: true);
        _businessMetrics.RecordFavoritesAction("add");

        return Ok(new
        {
            message = "Business metrics recorded",
            timestamp = DateTime.UtcNow
        });
    }
}