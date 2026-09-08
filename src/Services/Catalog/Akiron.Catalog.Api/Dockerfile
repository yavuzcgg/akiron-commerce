# Build context is the REPOSITORY ROOT, not this directory:
#   docker build -f src/Services/Catalog/Akiron.Catalog.Api/Dockerfile -t akiron-catalog .
# Central package management means restore needs Directory.Packages.props and
# Directory.Build.props, which live above the service.

FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

# Project files first: this layer is cached until a dependency actually changes, so
# editing source does not re-download the package graph.
COPY global.json Directory.Build.props Directory.Packages.props ./
COPY src/Services/Catalog/Akiron.Catalog.Domain/Akiron.Catalog.Domain.csproj src/Services/Catalog/Akiron.Catalog.Domain/
COPY src/Services/Catalog/Akiron.Catalog.Application/Akiron.Catalog.Application.csproj src/Services/Catalog/Akiron.Catalog.Application/
COPY src/Services/Catalog/Akiron.Catalog.Infrastructure/Akiron.Catalog.Infrastructure.csproj src/Services/Catalog/Akiron.Catalog.Infrastructure/
COPY src/Services/Catalog/Akiron.Catalog.Api/Akiron.Catalog.Api.csproj src/Services/Catalog/Akiron.Catalog.Api/
RUN dotnet restore src/Services/Catalog/Akiron.Catalog.Api/Akiron.Catalog.Api.csproj

# Only the four service projects: test sources have no business in a runtime image.
COPY src/Services/Catalog/Akiron.Catalog.Domain/ src/Services/Catalog/Akiron.Catalog.Domain/
COPY src/Services/Catalog/Akiron.Catalog.Application/ src/Services/Catalog/Akiron.Catalog.Application/
COPY src/Services/Catalog/Akiron.Catalog.Infrastructure/ src/Services/Catalog/Akiron.Catalog.Infrastructure/
COPY src/Services/Catalog/Akiron.Catalog.Api/ src/Services/Catalog/Akiron.Catalog.Api/
RUN dotnet publish src/Services/Catalog/Akiron.Catalog.Api/Akiron.Catalog.Api.csproj \
    --configuration Release \
    --no-restore \
    --output /app/publish \
    /p:UseAppHost=false

FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS final
WORKDIR /app

ENV ASPNETCORE_URLS=http://+:8080
EXPOSE 8080

COPY --from=build /app/publish .

# APP_UID is the non-root user the base image provides.
USER $APP_UID

ENTRYPOINT ["dotnet", "Akiron.Catalog.Api.dll"]
