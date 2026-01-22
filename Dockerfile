# =========================
# Runtime image
# =========================
FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS base
WORKDIR /app
EXPOSE 8080

# =========================
# Build image
# =========================
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

# Copy solution and project files (for restore caching)
COPY VidyaOSSol.sln .
COPY VidyaOSWebAPI/VidyaOSWebAPI.csproj VidyaOSWebAPI/
COPY VidyaOSDAL/VidyaOSDAL.csproj VidyaOSDAL/
COPY VidyaOSServices/VidyaOSServices.csproj VidyaOSServices/
COPY VidyaOSHelper/VidyaOSHelper.csproj VidyaOSHelper/

# Restore dependencies
RUN dotnet restore VidyaOSSol.sln

# Copy the rest of the source code
COPY . .

# Publish ONLY the Web API project
RUN dotnet publish VidyaOSWebAPI/VidyaOSWebAPI.csproj \
    -c Release \
    -o /app/publish \
    --no-restore

# =========================
# Final image
# =========================
FROM base AS final
WORKDIR /app
COPY --from=build /app/publish .
ENTRYPOINT ["dotnet", "VidyaOSWebAPI.dll"]
