FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS base
WORKDIR /app
EXPOSE 8080

FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src
COPY ["Session/FocusTrack.Session.Api/FocusTrack.Session.Api.csproj", "Session/FocusTrack.Session.Api/"]
COPY ["Session/FocusTrack.Session.Application/FocusTrack.Session.Application.csproj", "Session/FocusTrack.Session.Application/"]
COPY ["Session/FocusTrack.Session.Domain/FocusTrack.Session.Domain.csproj", "Session/FocusTrack.Session.Domain/"]
COPY ["Session/FocusTrack.Session.Infrastructure/FocusTrack.Session.Infrastructure.csproj", "Session/FocusTrack.Session.Infrastructure/"]
RUN dotnet restore "Session/FocusTrack.Session.Api/FocusTrack.Session.Api.csproj"
COPY Session/ Session/
WORKDIR /src/Session/FocusTrack.Session.Api
RUN dotnet build "FocusTrack.Session.Api.csproj" -c Release -o /app/build

FROM build AS publish
RUN dotnet publish "FocusTrack.Session.Api.csproj" -c Release -o /app/publish /p:UseAppHost=false

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "FocusTrack.Session.Api.dll"]
