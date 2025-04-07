FROM mcr.microsoft.com/dotnet/aspnet:9.0 AS base
USER $APP_UID
WORKDIR /app

FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build
ARG BUILD_CONFIGURATION=Release
WORKDIR /src
COPY ["NitroType.Team/NitroType.Team.csproj", "NitroType.Team/"]
RUN dotnet restore "NitroType.Team/NitroType.Team.csproj"
COPY . .
WORKDIR "/src/NitroType.Team"
RUN dotnet build "NitroType.Team.csproj" -c $BUILD_CONFIGURATION -o /app/build

FROM build AS publish
ARG BUILD_CONFIGURATION=Release
RUN dotnet publish "NitroType.Team.csproj" -c $BUILD_CONFIGURATION -o /app/publish /p:UseAppHost=false

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "NitroType.Team.dll"]
