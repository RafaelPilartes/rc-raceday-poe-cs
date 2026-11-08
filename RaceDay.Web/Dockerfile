# Build the app with the SDK image, then ship only the published output on the
# smaller ASP.NET runtime image.
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

# The project file is copied on its own first so restored packages stay cached
# when only source files change.
COPY RaceDay.Web/RaceDay.Web.csproj RaceDay.Web/
RUN dotnet restore RaceDay.Web/RaceDay.Web.csproj

COPY RaceDay.Web/ RaceDay.Web/
RUN dotnet publish RaceDay.Web/RaceDay.Web.csproj --configuration Release --no-restore --output /app/publish

FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS runtime
WORKDIR /app
COPY --from=build /app/publish .

ENV ASPNETCORE_URLS=http://+:8080
EXPOSE 8080

ENTRYPOINT ["dotnet", "RaceDay.Web.dll"]
