using Microsoft.EntityFrameworkCore;
using BetterMe.Infrastructure.Data;

namespace BetterMe.Infrastructure.Services;

public class DatabaseOpsService : IDatabaseOpsService
{
    private readonly AppDbContext _db;

    public DatabaseOpsService(AppDbContext db)
    {
        _db = db;
    }

    public async Task<DatabaseOpsSnapshot> GetSnapshotAsync(CancellationToken cancellationToken = default)
    {
        var canConnect = await _db.Database.CanConnectAsync(cancellationToken);
        if (!canConnect)
        {
            return new DatabaseOpsSnapshot(
                ServerVersion: "unavailable",
                CanConnect: false,
                AppliedMigrations: Array.Empty<string>(),
                PendingMigrations: Array.Empty<string>(),
                ActiveConnections: 0,
                PgStatStatementsAvailable: false);
        }

        var applied = await _db.Database.GetAppliedMigrationsAsync(cancellationToken);
        var pending = await _db.Database.GetPendingMigrationsAsync(cancellationToken);
        var version = await ScalarAsync("SHOW server_version", cancellationToken) ?? "unknown";
        var connections = await CountAsync(
            "SELECT COUNT(*) FROM pg_stat_activity WHERE datname = current_database()",
            cancellationToken);
        var statementsAvailable = await ExistsAsync(
            "SELECT 1 FROM pg_extension WHERE extname = 'pg_stat_statements'",
            cancellationToken);

        return new DatabaseOpsSnapshot(
            ServerVersion: version,
            CanConnect: true,
            AppliedMigrations: applied.ToList(),
            PendingMigrations: pending.ToList(),
            ActiveConnections: connections,
            PgStatStatementsAvailable: statementsAvailable);
    }

    private async Task<string?> ScalarAsync(string sql, CancellationToken cancellationToken)
    {
        await using var command = _db.Database.GetDbConnection().CreateCommand();
        command.CommandText = sql;
        await _db.Database.OpenConnectionAsync(cancellationToken);
        var result = await command.ExecuteScalarAsync(cancellationToken);
        return result?.ToString();
    }

    private async Task<int> CountAsync(string sql, CancellationToken cancellationToken)
    {
        var value = await ScalarAsync(sql, cancellationToken);
        return int.TryParse(value, out var count) ? count : 0;
    }

    private async Task<bool> ExistsAsync(string sql, CancellationToken cancellationToken)
    {
        var value = await ScalarAsync(sql, cancellationToken);
        return value is not null;
    }
}
