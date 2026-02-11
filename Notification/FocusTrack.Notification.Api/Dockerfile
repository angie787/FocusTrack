FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS base
WORKDIR /app
EXPOSE 8080

FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src
COPY ["Notification/FocusTrack.Notification.Api/FocusTrack.Notification.Api.csproj", "Notification/FocusTrack.Notification.Api/"]
RUN dotnet restore "Notification/FocusTrack.Notification.Api/FocusTrack.Notification.Api.csproj"
COPY Notification/FocusTrack.Notification.Api/ Notification/FocusTrack.Notification.Api/
WORKDIR /src/Notification/FocusTrack.Notification.Api
RUN dotnet build "FocusTrack.Notification.Api.csproj" -c Release -o /app/build

FROM build AS publish
RUN dotnet publish "FocusTrack.Notification.Api.csproj" -c Release -o /app/publish /p:UseAppHost=false

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "FocusTrack.Notification.Api.dll"]
