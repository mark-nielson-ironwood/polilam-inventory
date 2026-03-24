FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS base
WORKDIR /app
EXPOSE 8080

FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src
COPY PolilamInventory.sln .
COPY src/PolilamInventory.Web/PolilamInventory.Web.csproj src/PolilamInventory.Web/
COPY tests/PolilamInventory.Tests/PolilamInventory.Tests.csproj tests/PolilamInventory.Tests/
RUN dotnet restore
COPY . .
RUN dotnet test --no-restore -c Release
RUN dotnet publish src/PolilamInventory.Web -c Release -o /app/publish --no-restore

FROM base AS final
WORKDIR /app
COPY --from=build /app/publish .
ENV ASPNETCORE_URLS=http://+:8080
ENV ASPNETCORE_ENVIRONMENT=Production
ENTRYPOINT ["dotnet", "PolilamInventory.Web.dll"]
