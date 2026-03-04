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

# Copy ONLY required project files
COPY VidyaOSWebAPI/VidyaOSWebAPI.csproj VidyaOSWebAPI/
COPY VidyaOSDAL/VidyaOSDAL.csproj VidyaOSDAL/
COPY VidyaOSServices/VidyaOSServices.csproj VidyaOSServices/
COPY VidyaOSHelper/VidyaOSHelper.csproj VidyaOSHelper/

# Restore ONLY Web API (avoids ClientApp)
RUN dotnet restore VidyaOSWebAPI/VidyaOSWebAPI.csproj

# Copy full source
COPY . .

# Publish Web API
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
