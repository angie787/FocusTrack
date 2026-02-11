FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS base
WORKDIR /app

FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src
COPY ["RewardWorker/FocusTrack.RewardWorker/FocusTrack.RewardWorker.csproj", "RewardWorker/FocusTrack.RewardWorker/"]
RUN dotnet restore "RewardWorker/FocusTrack.RewardWorker/FocusTrack.RewardWorker.csproj"
COPY RewardWorker/FocusTrack.RewardWorker/ RewardWorker/FocusTrack.RewardWorker/
WORKDIR /src/RewardWorker/FocusTrack.RewardWorker
RUN dotnet build "FocusTrack.RewardWorker.csproj" -c Release -o /app/build

FROM build AS publish
RUN dotnet publish "FocusTrack.RewardWorker.csproj" -c Release -o /app/publish /p:UseAppHost=false

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "FocusTrack.RewardWorker.dll"]
