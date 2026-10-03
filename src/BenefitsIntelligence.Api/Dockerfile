# Build context is the repository root so the shared build props and global.json are available.

FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

# Project files first: the restore layer is only rebuilt when dependencies change.
COPY global.json Directory.Build.props Directory.Packages.props .editorconfig ./
COPY src/BenefitsIntelligence.Domain/BenefitsIntelligence.Domain.csproj src/BenefitsIntelligence.Domain/
COPY src/BenefitsIntelligence.Application/BenefitsIntelligence.Application.csproj src/BenefitsIntelligence.Application/
COPY src/BenefitsIntelligence.Infrastructure/BenefitsIntelligence.Infrastructure.csproj src/BenefitsIntelligence.Infrastructure/
COPY src/BenefitsIntelligence.Api/BenefitsIntelligence.Api.csproj src/BenefitsIntelligence.Api/
RUN dotnet restore src/BenefitsIntelligence.Api/BenefitsIntelligence.Api.csproj

COPY src/ src/
RUN dotnet publish src/BenefitsIntelligence.Api/BenefitsIntelligence.Api.csproj -c Release -o /app/publish --no-restore

FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS runtime
WORKDIR /app

# Created with the runtime user's ownership so an empty named volume mounted here inherits it.
RUN mkdir /data && chown $APP_UID /data

COPY --from=build /app/publish .

USER $APP_UID
ENTRYPOINT ["dotnet", "BenefitsIntelligence.Api.dll"]
