# Build context is the REPOSITORY ROOT, not this directory:
#   docker build -f src/Services/Identity/Akiron.Identity.Api/Dockerfile -t akiron-identity .
# Central package management means restore needs Directory.Packages.props and
# Directory.Build.props, which live above the service.

FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

# Project files first: this layer is cached until a dependency actually changes, so
# editing source does not re-download the package graph.
COPY global.json Directory.Build.props Directory.Packages.props ./
COPY src/Services/Identity/Akiron.Identity.Domain/Akiron.Identity.Domain.csproj src/Services/Identity/Akiron.Identity.Domain/
COPY src/Services/Identity/Akiron.Identity.Application/Akiron.Identity.Application.csproj src/Services/Identity/Akiron.Identity.Application/
COPY src/Services/Identity/Akiron.Identity.Infrastructure/Akiron.Identity.Infrastructure.csproj src/Services/Identity/Akiron.Identity.Infrastructure/
COPY src/Services/Identity/Akiron.Identity.Api/Akiron.Identity.Api.csproj src/Services/Identity/Akiron.Identity.Api/
RUN dotnet restore src/Services/Identity/Akiron.Identity.Api/Akiron.Identity.Api.csproj

# Only the four service projects: test sources have no business in a runtime image.
COPY src/Services/Identity/Akiron.Identity.Domain/ src/Services/Identity/Akiron.Identity.Domain/
COPY src/Services/Identity/Akiron.Identity.Application/ src/Services/Identity/Akiron.Identity.Application/
COPY src/Services/Identity/Akiron.Identity.Infrastructure/ src/Services/Identity/Akiron.Identity.Infrastructure/
COPY src/Services/Identity/Akiron.Identity.Api/ src/Services/Identity/Akiron.Identity.Api/
RUN dotnet publish src/Services/Identity/Akiron.Identity.Api/Akiron.Identity.Api.csproj \
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

ENTRYPOINT ["dotnet", "Akiron.Identity.Api.dll"]
