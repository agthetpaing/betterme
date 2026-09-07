namespace BetterMe.Infrastructure.Services;

public interface IDatabaseOpsService
{
    Task<DatabaseOpsSnapshot> GetSnapshotAsync(CancellationToken cancellationToken = default);
}

public sealed record DatabaseOpsSnapshot(
    string ServerVersion,
    bool CanConnect,
    IReadOnlyList<string> AppliedMigrations,
    IReadOnlyList<string> PendingMigrations,
    int ActiveConnections,
    bool PgStatStatementsAvailable);
