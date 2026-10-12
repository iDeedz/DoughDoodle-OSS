# DoughDoodle — multi-stage .NET 8 image
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src
COPY DoughDoodle.csproj .
RUN dotnet restore
COPY . .
RUN dotnet publish -c Release -o /app/publish --no-restore

FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS final
WORKDIR /app
ENV ASPNETCORE_URLS=http://+:8080
ENV Seed__AllowDemoUsers=false
EXPOSE 8080
COPY --from=build /app/publish .
HEALTHCHECK --interval=30s --timeout=5s --start-period=20s CMD curl -f http://localhost:8080/health || exit 1
ENTRYPOINT ["dotnet", "DoughDoodle.dll"]
