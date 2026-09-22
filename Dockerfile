FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src
ENV DOTNET_CLI_TELEMETRY_OPTOUT=1 \
    DOTNET_NOLOGO=1 \
    NUGET_XMLDOC_MODE=skip \
    NuGetAudit=false

COPY ["TORSEPAN.API/TORSEPAN.API.csproj", "TORSEPAN.API/"]
COPY ["TORSEPAN.Application/TORSEPAN.Application.csproj", "TORSEPAN.Application/"]
COPY ["TORSEPAN.Domain/TORSEPAN.Domain.csproj", "TORSEPAN.Domain/"]
COPY ["TORSEPAN.Infrastructure/TORSEPAN.Infrastructure.csproj", "TORSEPAN.Infrastructure/"]
RUN dotnet restore "TORSEPAN.API/TORSEPAN.API.csproj" \
      --source "https://package-mirror.liara.ir/repository/nuget/index.json" \
      --disable-parallel

COPY TORSEPAN.API/ TORSEPAN.API/
COPY TORSEPAN.Application/ TORSEPAN.Application/
COPY TORSEPAN.Domain/ TORSEPAN.Domain/
COPY TORSEPAN.Infrastructure/ TORSEPAN.Infrastructure/
RUN dotnet publish "TORSEPAN.API/TORSEPAN.API.csproj" \
      -c Release -o /app/publish --no-restore \
      /p:UseAppHost=false \
      /p:RunAnalyzers=false \
      /p:DebugType=None \
      /p:DebugSymbols=false

FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS final
USER root
RUN apt-get update \
    && apt-get install -y --no-install-recommends fonts-dejavu-core postgresql-client \
    && rm -rf /var/lib/apt/lists/*
WORKDIR /app
ENV ASPNETCORE_URLS=http://0.0.0.0:8080 \
    ASPNETCORE_ENVIRONMENT=Production
EXPOSE 8080
COPY --from=build /app/publish .
USER $APP_UID
ENTRYPOINT ["dotnet", "TORSEPAN.API.dll"]
