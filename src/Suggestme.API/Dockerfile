# Build stage
FROM mcr.microsoft.com/dotnet/sdk:7.0 AS build
WORKDIR /src

# Copy project files and restore
COPY Suggestme.Shared/Suggestme.Shared.csproj Suggestme.Shared/
COPY Suggestme.Infrastructure/Suggestme.Infrastructure.csproj Suggestme.Infrastructure/
COPY Suggestme.API/Suggestme.API.csproj Suggestme.API/
RUN dotnet restore Suggestme.API/Suggestme.API.csproj

# Copy source and publish
COPY Suggestme.Shared/ Suggestme.Shared/
COPY Suggestme.Infrastructure/ Suggestme.Infrastructure/
COPY Suggestme.API/ Suggestme.API/
RUN dotnet publish Suggestme.API/Suggestme.API.csproj -c Release -o /app/publish

# Runtime stage
FROM mcr.microsoft.com/dotnet/aspnet:7.0 AS runtime
WORKDIR /app
COPY --from=build /app/publish .

EXPOSE 8080
ENV ASPNETCORE_URLS=http://+:8080

ENTRYPOINT ["dotnet", "Suggestme.API.dll"]
