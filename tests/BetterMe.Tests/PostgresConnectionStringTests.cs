using Npgsql;
using BetterMe.Infrastructure.Data;

namespace BetterMe.Tests;

public class PostgresConnectionStringTests
{
    [Fact]
    public void Require_without_trust_sets_TrustServerCertificate()
    {
        var result = PostgresConnectionString.Normalize(
            "Host=psql.example;Database=betterme;Username=u;Password=p;SSL Mode=Require");

        var builder = new NpgsqlConnectionStringBuilder(result);
        Assert.Equal(SslMode.Require, builder.SslMode);
        Assert.True(builder.TrustServerCertificate);
    }

    [Fact]
    public void Local_connection_string_does_not_enable_trust()
    {
        var result = PostgresConnectionString.Normalize(
            "Host=localhost;Database=betterme;Username=betterme_user;Password=localdevpassword");

        var builder = new NpgsqlConnectionStringBuilder(result);
        Assert.False(builder.TrustServerCertificate);
    }
}
