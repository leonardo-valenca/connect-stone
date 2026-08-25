# Build context is the repo root (see docker-compose.yml) so this can reach src/sdk.
FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

COPY src/sdk/ConnectStone.Sdk/ConnectStone.Sdk.csproj src/sdk/ConnectStone.Sdk/
COPY src/backend/Domain/Domain.csproj src/backend/Domain/
COPY src/backend/Application/Application.csproj src/backend/Application/
COPY src/backend/Infrastructure/Infrastructure.csproj src/backend/Infrastructure/
COPY src/backend/Api/Api.csproj src/backend/Api/
RUN dotnet restore src/backend/Api/Api.csproj

COPY src/sdk/ConnectStone.Sdk/ src/sdk/ConnectStone.Sdk/
COPY src/backend/Domain/ src/backend/Domain/
COPY src/backend/Application/ src/backend/Application/
COPY src/backend/Infrastructure/ src/backend/Infrastructure/
COPY src/backend/Api/ src/backend/Api/
RUN dotnet publish src/backend/Api/Api.csproj -c Release -o /app/publish

FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS runtime
WORKDIR /app

RUN apt-get update \
    && apt-get install -y --no-install-recommends curl \
    && rm -rf /var/lib/apt/lists/*

RUN mkdir -p /data && chown $APP_UID:$APP_UID /data
USER $APP_UID

COPY --from=build /app/publish .

ENV ASPNETCORE_URLS=http://+:8080
EXPOSE 8080

ENTRYPOINT ["dotnet", "Api.dll"]