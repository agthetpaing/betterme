using Microsoft.Extensions.Configuration;
using Npgsql;

namespace BetterMe.Infrastructure.Data;

public static class PostgresConnectionString
{
    public static string FromConfiguration(IConfiguration config)
    {
        var connectionString = config.GetConnectionString("DefaultConnection")
            ?? throw new InvalidOperationException("Connection string 'DefaultConnection' is not configured.");

        return Normalize(connectionString);
    }

    public static string Normalize(string connectionString)
    {
        var builder = new NpgsqlConnectionStringBuilder(connectionString);
        if (builder.SslMode == SslMode.Require)
            builder.TrustServerCertificate = true;

        return builder.ToString();
    }
}
