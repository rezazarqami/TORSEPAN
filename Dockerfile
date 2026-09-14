FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

COPY ["TORSEPAN.API/TORSEPAN.API.csproj", "TORSEPAN.API/"]
COPY ["TORSEPAN.Panel/TORSEPAN.Panel.csproj", "TORSEPAN.Panel/"]
COPY ["TORSEPAN.Application/TORSEPAN.Application.csproj", "TORSEPAN.Application/"]
COPY ["TORSEPAN.Domain/TORSEPAN.Domain.csproj", "TORSEPAN.Domain/"]
COPY ["TORSEPAN.Infrastructure/TORSEPAN.Infrastructure.csproj", "TORSEPAN.Infrastructure/"]
RUN dotnet restore "TORSEPAN.API/TORSEPAN.API.csproj" && \
    dotnet restore "TORSEPAN.Panel/TORSEPAN.Panel.csproj"

COPY . .
RUN dotnet publish "TORSEPAN.API/TORSEPAN.API.csproj" -c Release -o /app/api --no-restore /p:UseAppHost=false && \
    dotnet publish "TORSEPAN.Panel/TORSEPAN.Panel.csproj" -c Release -o /app/panel --no-restore /p:UseAppHost=false

FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS final
USER root
RUN apt-get update \
    && apt-get install -y --no-install-recommends ca-certificates curl fonts-dejavu-core \
    && install -d /usr/share/postgresql-common/pgdg \
    && curl -fsSL https://www.postgresql.org/media/keys/ACCC4CF8.asc \
       -o /usr/share/postgresql-common/pgdg/apt.postgresql.org.asc \
    && echo "deb [signed-by=/usr/share/postgresql-common/pgdg/apt.postgresql.org.asc] https://apt.postgresql.org/pub/repos/apt noble-pgdg main" \
       > /etc/apt/sources.list.d/pgdg.list \
    && apt-get update \
    && apt-get install -y --no-install-recommends postgresql-client-17 \
    && rm -rf /var/lib/apt/lists/*
WORKDIR /app
ENV ASPNETCORE_URLS=http://0.0.0.0:8080 \
    ASPNETCORE_ENVIRONMENT=Production
EXPOSE 8080
COPY --from=build /app/api ./api
COPY --from=build /app/panel ./panel
USER $APP_UID
ENTRYPOINT ["sh", "-c", "if [ \"$TORSEPAN_SERVICE\" = \"api\" ]; then exec dotnet /app/api/TORSEPAN.API.dll; elif [ \"$TORSEPAN_SERVICE\" = \"panel\" ]; then exec dotnet /app/panel/TORSEPAN.Panel.dll; else echo 'TORSEPAN_SERVICE must be api or panel' >&2; exit 1; fi"]
