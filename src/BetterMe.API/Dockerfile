# Build stage
FROM mcr.microsoft.com/dotnet/sdk:7.0 AS build
WORKDIR /src

# Copy project files and restore
COPY BetterMe.Shared/BetterMe.Shared.csproj BetterMe.Shared/
COPY BetterMe.Infrastructure/BetterMe.Infrastructure.csproj BetterMe.Infrastructure/
COPY BetterMe.API/BetterMe.API.csproj BetterMe.API/
RUN dotnet restore BetterMe.API/BetterMe.API.csproj

# Copy source and publish
COPY BetterMe.Shared/ BetterMe.Shared/
COPY BetterMe.Infrastructure/ BetterMe.Infrastructure/
COPY BetterMe.API/ BetterMe.API/
RUN dotnet publish BetterMe.API/BetterMe.API.csproj -c Release -o /app/publish

# Runtime stage
FROM mcr.microsoft.com/dotnet/aspnet:7.0 AS runtime
WORKDIR /app
COPY --from=build /app/publish .

EXPOSE 8080
ENV ASPNETCORE_URLS=http://+:8080

ENTRYPOINT ["dotnet", "BetterMe.API.dll"]
