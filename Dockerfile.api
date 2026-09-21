# syntax=docker/dockerfile:1.7
FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

COPY ["TORSEPAN.API/TORSEPAN.API.csproj", "TORSEPAN.API/"]
COPY ["TORSEPAN.Application/TORSEPAN.Application.csproj", "TORSEPAN.Application/"]
COPY ["TORSEPAN.Domain/TORSEPAN.Domain.csproj", "TORSEPAN.Domain/"]
COPY ["TORSEPAN.Infrastructure/TORSEPAN.Infrastructure.csproj", "TORSEPAN.Infrastructure/"]
RUN --mount=type=cache,id=torsepan-nuget,target=/root/.nuget/packages \
    dotnet restore "TORSEPAN.API/TORSEPAN.API.csproj"

COPY TORSEPAN.API/ TORSEPAN.API/
COPY TORSEPAN.Application/ TORSEPAN.Application/
COPY TORSEPAN.Domain/ TORSEPAN.Domain/
COPY TORSEPAN.Infrastructure/ TORSEPAN.Infrastructure/
RUN --mount=type=cache,id=torsepan-nuget,target=/root/.nuget/packages \
    dotnet publish "TORSEPAN.API/TORSEPAN.API.csproj" -c Release -o /app/publish --no-restore /p:UseAppHost=false

FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS final
USER root
ADD https://www.postgresql.org/media/keys/ACCC4CF8.asc /tmp/pgdg.asc
RUN install -d /usr/share/postgresql-common/pgdg \
    && mv /tmp/pgdg.asc /usr/share/postgresql-common/pgdg/apt.postgresql.org.asc \
    && echo "deb [signed-by=/usr/share/postgresql-common/pgdg/apt.postgresql.org.asc] https://apt.postgresql.org/pub/repos/apt noble-pgdg main" \
       > /etc/apt/sources.list.d/pgdg.list \
    && apt-get update \
    && apt-get install -y --no-install-recommends fonts-dejavu-core postgresql-client-17 \
    && rm -rf /var/lib/apt/lists/*
WORKDIR /app
ENV ASPNETCORE_URLS=http://0.0.0.0:8080 \
    ASPNETCORE_ENVIRONMENT=Production
EXPOSE 8080
COPY --from=build /app/publish .
USER $APP_UID
ENTRYPOINT ["dotnet", "TORSEPAN.API.dll"]
