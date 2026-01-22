FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS base
WORKDIR /app
EXPOSE 8080

FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

# Copy solution and project files
COPY VidyaOS.sln .
COPY VidyaOSWebAPI/VidyaOSWebAPI.csproj VidyaOSWebAPI/
COPY VidyaOSDAL/VidyaOSDAL.csproj VidyaOSDAL/
COPY VidyaOSServices/VidyaOSServices.csproj VidyaOSServices/
COPY VidyaOSHelper/VidyaOSHelper.csproj VidyaOSHelper/

RUN dotnet restore

# Copy everything else
COPY . .

# Publish API project
RUN dotnet publish VidyaOSWebAPI/VidyaOSWebAPI.csproj -c Release -o /app/publish

FROM base AS final
WORKDIR /app
COPY --from=build /app/publish .
ENTRYPOINT ["dotnet", "VidyaOSWebAPI.dll"]
