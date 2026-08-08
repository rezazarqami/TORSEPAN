FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

COPY ["TORSEPAN.Panel/TORSEPAN.Panel.csproj", "TORSEPAN.Panel/"]
COPY ["TORSEPAN.Application/TORSEPAN.Application.csproj", "TORSEPAN.Application/"]
COPY ["TORSEPAN.Domain/TORSEPAN.Domain.csproj", "TORSEPAN.Domain/"]
RUN dotnet restore "TORSEPAN.Panel/TORSEPAN.Panel.csproj"

COPY . .
RUN dotnet publish "TORSEPAN.Panel/TORSEPAN.Panel.csproj" -c Release -o /app/publish --no-restore /p:UseAppHost=false

FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS final
WORKDIR /app
ENV ASPNETCORE_URLS=http://0.0.0.0:8080 \
    ASPNETCORE_ENVIRONMENT=Production
EXPOSE 8080
COPY --from=build /app/publish .
USER $APP_UID
ENTRYPOINT ["dotnet", "TORSEPAN.Panel.dll"]
