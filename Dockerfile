FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src
ENV DOTNET_CLI_TELEMETRY_OPTOUT=1 \
    DOTNET_NOLOGO=1 \
    NUGET_XMLDOC_MODE=skip

COPY ["TORSEPAN.API/TORSEPAN.API.csproj", "TORSEPAN.API/"]
COPY ["TORSEPAN.Panel/TORSEPAN.Panel.csproj", "TORSEPAN.Panel/"]
COPY ["TORSEPAN.Application/TORSEPAN.Application.csproj", "TORSEPAN.Application/"]
COPY ["TORSEPAN.Domain/TORSEPAN.Domain.csproj", "TORSEPAN.Domain/"]
COPY ["TORSEPAN.Infrastructure/TORSEPAN.Infrastructure.csproj", "TORSEPAN.Infrastructure/"]
RUN dotnet restore "TORSEPAN.API/TORSEPAN.API.csproj" \
    && dotnet restore "TORSEPAN.Panel/TORSEPAN.Panel.csproj"

COPY TORSEPAN.API/ TORSEPAN.API/
COPY TORSEPAN.Panel/ TORSEPAN.Panel/
COPY TORSEPAN.Application/ TORSEPAN.Application/
COPY TORSEPAN.Domain/ TORSEPAN.Domain/
COPY TORSEPAN.Infrastructure/ TORSEPAN.Infrastructure/
RUN dotnet publish "TORSEPAN.API/TORSEPAN.API.csproj" \
      -c Release -o /app/api --no-restore \
      /p:UseAppHost=false \
      /p:RunAnalyzers=false \
      /p:DebugType=None \
      /p:DebugSymbols=false
RUN dotnet publish "TORSEPAN.Panel/TORSEPAN.Panel.csproj" \
      -c Release -o /app/panel --no-restore \
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
COPY --from=build /app/api ./api
COPY --from=build /app/panel ./panel
USER $APP_UID
ENTRYPOINT ["sh", "-c", "if [ \"$TORSEPAN_SERVICE\" = \"api\" ]; then cd /app/api && exec dotnet TORSEPAN.API.dll; elif [ \"$TORSEPAN_SERVICE\" = \"panel\" ]; then cd /app/panel && exec dotnet TORSEPAN.Panel.dll; else echo 'TORSEPAN_SERVICE must be api or panel' >&2; exit 1; fi"]
