using BetterMe.API.Extensions;
using BetterMe.Infrastructure.Extensions;

var builder = WebApplication.CreateBuilder(args);

builder.AddApiObservability();
builder.Services.AddInfrastructure(builder.Configuration);
builder.Services.AddApiAuthentication(builder.Configuration);
builder.Services.AddApiCors(builder.Configuration);
builder.Services.AddApiHealthChecks(builder.Configuration);
builder.Services.AddControllers();
builder.Services.AddApiSwagger();

var app = builder.Build();

// Schema is applied with: dotnet ef database update
// Do not Migrate() here — replicas must not race schema changes.
await app.SeedDevelopmentDataAsync();

app.UseApiObservability();
app.UseApiSwagger();
app.UseCors("BlazorPolicy");
app.UseHttpsRedirection();
app.UseAuthentication();
app.UseAuthorization();
app.MapControllers();
app.MapApiHealthChecks();

app.Run();
