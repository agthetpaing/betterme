using System.Text.Json;
using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.HttpLogging;

namespace BetterMe.API.Extensions;

public static class ObservabilityExtensions
{
    public static WebApplicationBuilder AddApiObservability(this WebApplicationBuilder builder)
    {
        builder.Logging.ClearProviders();
        builder.Logging.AddJsonConsole(options =>
        {
            options.IncludeScopes = true;
            options.TimestampFormat = "O";
        });

        builder.Services.AddHttpLogging(options =>
        {
            options.LoggingFields =
                HttpLoggingFields.RequestMethod |
                HttpLoggingFields.RequestPath |
                HttpLoggingFields.ResponseStatusCode;
            options.RequestBodyLogLimit = 0;
            options.ResponseBodyLogLimit = 0;
        });

        return builder;
    }

    public static WebApplication UseApiObservability(this WebApplication app)
    {
        app.UseExceptionHandler(errorApp =>
        {
            errorApp.Run(async context =>
            {
                var feature = context.Features.Get<IExceptionHandlerFeature>();
                var exception = feature?.Error;
                var logger = context.RequestServices.GetRequiredService<ILoggerFactory>()
                    .CreateLogger("BetterMe.API.ExceptionHandler");

                if (exception is UnauthorizedAccessException)
                {
                    logger.LogWarning(
                        exception,
                        "Unauthorised access on {Method} {Path}",
                        context.Request.Method,
                        context.Request.Path);

                    context.Response.StatusCode = StatusCodes.Status401Unauthorized;
                    context.Response.ContentType = "application/json";
                    await context.Response.WriteAsync(
                        JsonSerializer.Serialize(new { error = "Unauthorised." }));
                    return;
                }

                logger.LogError(
                    exception,
                    "Unhandled exception on {Method} {Path}",
                    context.Request.Method,
                    context.Request.Path);

                context.Response.StatusCode = StatusCodes.Status500InternalServerError;
                context.Response.ContentType = "application/json";
                await context.Response.WriteAsync(
                    JsonSerializer.Serialize(new { error = "An unexpected error occurred." }));
            });
        });

        app.UseWhen(
            ctx => !IsHealthPath(ctx.Request.Path),
            branch => branch.UseHttpLogging());

        return app;
    }

    private static bool IsHealthPath(PathString path) =>
        path.StartsWithSegments("/health") || path.StartsWithSegments("/ready");
}
