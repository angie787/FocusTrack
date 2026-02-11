FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS base
WORKDIR /app
EXPOSE 8080

FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src
COPY ["Gateway/FocusTrack.Gateway.Api/FocusTrack.Gateway.Api.csproj", "Gateway/FocusTrack.Gateway.Api/"]
RUN dotnet restore "Gateway/FocusTrack.Gateway.Api/FocusTrack.Gateway.Api.csproj"
COPY Gateway/FocusTrack.Gateway.Api/ Gateway/FocusTrack.Gateway.Api/
WORKDIR /src/Gateway/FocusTrack.Gateway.Api
RUN dotnet build "FocusTrack.Gateway.Api.csproj" -c Release -o /app/build

FROM build AS publish
RUN dotnet publish "FocusTrack.Gateway.Api.csproj" -c Release -o /app/publish /p:UseAppHost=false

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "FocusTrack.Gateway.Api.dll"]
