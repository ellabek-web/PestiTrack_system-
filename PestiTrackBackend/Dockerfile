# syntax=docker/dockerfile:1

FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

COPY PestiTrackBackend.csproj ./
RUN dotnet restore PestiTrackBackend.csproj

COPY . ./
RUN dotnet publish PestiTrackBackend.csproj -c Release -o /app/publish /p:UseAppHost=false

FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS final
WORKDIR /app

ENV ASPNETCORE_URLS=http://+:8080 \
    ASPNETCORE_ENVIRONMENT=Production

COPY --from=build /app/publish ./

# Entrypoint: applies EF Core migrations on start (idempotent) and
# listens on $PORT when the host sets it (Render free web services do).
COPY entrypoint.sh ./
RUN chmod +x entrypoint.sh

EXPOSE 8080
USER $APP_UID

ENTRYPOINT ["./entrypoint.sh"]
