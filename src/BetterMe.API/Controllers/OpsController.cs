using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using BetterMe.Infrastructure.Services;

namespace BetterMe.API.Controllers;

[ApiController]
[Route("api/ops")]
[Authorize(Roles = "Admin")]
public class OpsController : ControllerBase
{
    private readonly IDatabaseOpsService _databaseOps;
    private readonly ILogger<OpsController> _logger;

    public OpsController(IDatabaseOpsService databaseOps, ILogger<OpsController> logger)
    {
        _databaseOps = databaseOps;
        _logger = logger;
    }

    [HttpGet("database")]
    public async Task<IActionResult> GetDatabase(CancellationToken cancellationToken)
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier)
            ?? User.FindFirstValue("sub")
            ?? "unknown";

        _logger.LogInformation("Admin {UserId} requested database ops snapshot", userId);

        var snapshot = await _databaseOps.GetSnapshotAsync(cancellationToken);
        return Ok(snapshot);
    }
}
