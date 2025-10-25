
FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS base
USER app
WORKDIR /app
EXPOSE 8080
EXPOSE 8081

FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
ARG BUILD_CONFIGURATION=Release
WORKDIR /src
COPY ["EngAce.Api/EngAce.Api.csproj", "EngAce.Api/"]
COPY ["Events/Events.csproj", "Events/"]
COPY ["Entities/Entities.csproj", "Entities/"]
COPY ["Helper/Helper.csproj", "Helper/"]
RUN dotnet restore "./EngAce.Api/EngAce.Api.csproj"
COPY . .
WORKDIR "/src/EngAce.Api"
RUN dotnet build "./EngAce.Api.csproj" -c $BUILD_CONFIGURATION -o /app/build

FROM build AS publish
ARG BUILD_CONFIGURATION=Release
RUN dotnet publish "./EngAce.Api.csproj" -c $BUILD_CONFIGURATION -o /app/publish /p:UseAppHost=false

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "EngAce.Api.dll"]