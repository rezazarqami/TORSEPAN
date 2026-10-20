FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src
ENV DOTNET_CLI_TELEMETRY_OPTOUT=1 \
    DOTNET_NOLOGO=1 \
    NUGET_XMLDOC_MODE=skip \
    NuGetAudit=false
ARG TORSEPAN_NUGET_SOURCE=https://package-mirror.liara.ir/repository/nuget/index.json

COPY ["TORSEPAN.API/TORSEPAN.API.csproj", "TORSEPAN.API/"]
COPY ["TORSEPAN.Panel/TORSEPAN.Panel.csproj", "TORSEPAN.Panel/"]
COPY ["TORSEPAN.Application/TORSEPAN.Application.csproj", "TORSEPAN.Application/"]
COPY ["TORSEPAN.Domain/TORSEPAN.Domain.csproj", "TORSEPAN.Domain/"]
COPY ["TORSEPAN.Infrastructure/TORSEPAN.Infrastructure.csproj", "TORSEPAN.Infrastructure/"]
RUN dotnet restore "TORSEPAN.API/TORSEPAN.API.csproj" \
      --source "$TORSEPAN_NUGET_SOURCE" \
      --disable-parallel
RUN dotnet restore "TORSEPAN.Panel/TORSEPAN.Panel.csproj" \
      --source "$TORSEPAN_NUGET_SOURCE" \
      --disable-parallel

COPY TORSEPAN.API/ TORSEPAN.API/
COPY TORSEPAN.Panel/ TORSEPAN.Panel/
COPY TORSEPAN.Application/ TORSEPAN.Application/
COPY TORSEPAN.Domain/ TORSEPAN.Domain/
COPY TORSEPAN.Infrastructure/ TORSEPAN.Infrastructure/
RUN dotnet publish "TORSEPAN.API/TORSEPAN.API.csproj" \
      -c Release -o /app/publish/api --no-restore \
      /p:UseAppHost=false \
      /p:RunAnalyzers=false \
      /p:DebugType=None \
      /p:DebugSymbols=false
RUN dotnet publish "TORSEPAN.Panel/TORSEPAN.Panel.csproj" \
      -c Release -o /app/publish/panel --no-restore \
      /p:UseAppHost=false \
      /p:RunAnalyzers=false \
      /p:DebugType=None \
      /p:DebugSymbols=false

FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS final
USER root
RUN apt-get update \
    && apt-get install -y --no-install-recommends ca-certificates curl fonts-dejavu-core \
    && install -d /usr/share/postgresql-common/pgdg \
    && curl -fsSL https://www.postgresql.org/media/keys/ACCC4CF8.asc \
       -o /usr/share/postgresql-common/pgdg/apt.postgresql.org.asc \
    && . /etc/os-release \
    && echo "deb [signed-by=/usr/share/postgresql-common/pgdg/apt.postgresql.org.asc] https://apt.postgresql.org/pub/repos/apt ${VERSION_CODENAME}-pgdg main" \
       > /etc/apt/sources.list.d/pgdg.list \
    && apt-get update \
    && apt-get install -y --no-install-recommends postgresql-client-17 \
    && pg_dump --version | grep -E '^pg_dump \(PostgreSQL\) 17\.' \
    && rm -rf /var/lib/apt/lists/*
WORKDIR /app
ENV ASPNETCORE_URLS=http://0.0.0.0:8080 \
    ASPNETCORE_ENVIRONMENT=Production
EXPOSE 8080
COPY --from=build /app/publish .
COPY start-service.sh /app/start-service.sh
USER $APP_UID
ENTRYPOINT ["/bin/sh", "/app/start-service.sh"]
