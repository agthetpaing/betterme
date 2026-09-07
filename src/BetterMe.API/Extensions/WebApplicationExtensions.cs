using Microsoft.AspNetCore.Diagnostics.HealthChecks;
using BetterMe.Infrastructure.Data;

namespace BetterMe.API.Extensions;

public static class WebApplicationExtensions
{
    public static async Task SeedDevelopmentDataAsync(this WebApplication app)
    {
        if (!app.Environment.IsDevelopment())
            return;

        using var scope = app.Services.CreateScope();
        await DataSeeder.SeedRolesAsync(scope.ServiceProvider);
        await DataSeeder.SeedResourcesAsync(scope.ServiceProvider);
    }

    public static WebApplication UseApiSwagger(this WebApplication app)
    {
        if (app.Environment.IsDevelopment())
        {
            app.UseSwagger();
            app.UseSwaggerUI();
        }

        return app;
    }

    public static WebApplication MapApiHealthChecks(this WebApplication app)
    {
        app.MapHealthChecks("/health", new HealthCheckOptions { Predicate = _ => false })
            .AllowAnonymous();
        app.MapHealthChecks("/ready", new HealthCheckOptions
        {
            Predicate = check => check.Tags.Contains("ready")
        }).AllowAnonymous();

        return app;
    }
}
