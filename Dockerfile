# RaceDay MVC web app (Part 3).
# Build:  docker build -t raceday-web .
# Run:    docker run -d -p 8080:8080 --name raceday-web raceday-web
# Then open http://localhost:8080. The Part 2 API must be running on the host at http://localhost:5088.

# ---- Stage 1: restore and publish with the full .NET SDK ----
FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

# Copy only the project file and NuGet settings first. Docker caches the restore layer,
# so it is reused until a package reference changes.
COPY nuget.config ./
COPY RaceDayWeb/RaceDayWeb.csproj RaceDayWeb/
RUN dotnet restore RaceDayWeb/RaceDayWeb.csproj

# Copy the rest of the MVC source and publish a Release build.
COPY RaceDayWeb/ RaceDayWeb/
RUN dotnet publish RaceDayWeb/RaceDayWeb.csproj --configuration Release --no-restore --output /app/publish /p:UseAppHost=false

# ---- Stage 2: run on the smaller ASP.NET Core runtime image ----
FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS final
WORKDIR /app

# Listen on port 8080 inside the container.
ENV ASPNETCORE_URLS=http://+:8080 \
    ASPNETCORE_ENVIRONMENT=Production \
    # The container reaches the API running on the host through host.docker.internal.
    Api__BaseUrl=http://host.docker.internal:5088 \
    # The browser runs on the host, so it loads API-hosted images from localhost.
    Api__PublicUrl=http://localhost:5088

EXPOSE 8080

COPY --from=build /app/publish .

# Run as the image's built-in non-root user instead of root.
USER $APP_UID

ENTRYPOINT ["dotnet", "RaceDayWeb.dll"]
