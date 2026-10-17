FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build
WORKDIR /src

COPY backend/IncidentCopilot.Core/IncidentCopilot.Core.csproj backend/IncidentCopilot.Core/
COPY backend/IncidentCopilot.Application/IncidentCopilot.Application.csproj backend/IncidentCopilot.Application/
COPY backend/IncidentCopilot.Infrastructure/IncidentCopilot.Infrastructure.csproj backend/IncidentCopilot.Infrastructure/
COPY backend/IncidentCopilot.Api/IncidentCopilot.Api.csproj backend/IncidentCopilot.Api/
RUN dotnet restore backend/IncidentCopilot.Api/IncidentCopilot.Api.csproj

COPY backend ./backend
COPY docs ./docs
RUN dotnet publish backend/IncidentCopilot.Api/IncidentCopilot.Api.csproj \
    --configuration Release \
    --no-restore \
    --output /out

FROM mcr.microsoft.com/dotnet/aspnet:9.0 AS runtime
RUN apt-get update \
    && apt-get install --yes --no-install-recommends curl \
    && rm -rf /var/lib/apt/lists/*
WORKDIR /app
COPY --from=build /out .
COPY --from=build /src/docs /docs

ENV ASPNETCORE_HTTP_PORTS=8080
EXPOSE 8080

ENTRYPOINT ["dotnet", "IncidentCopilot.Api.dll"]
