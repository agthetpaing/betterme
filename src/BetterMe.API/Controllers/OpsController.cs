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

    public OpsController(IDatabaseOpsService databaseOps)
    {
        _databaseOps = databaseOps;
    }

    [HttpGet("database")]
    public async Task<IActionResult> GetDatabase(CancellationToken cancellationToken)
    {
        var snapshot = await _databaseOps.GetSnapshotAsync(cancellationToken);
        return Ok(snapshot);
    }
}
