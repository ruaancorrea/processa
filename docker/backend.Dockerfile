# syntax=docker/dockerfile:1
FROM mcr.microsoft.com/dotnet/sdk:10.0-alpine AS build
WORKDIR /src

COPY backend/Processa.slnx ./
COPY backend/src ./src
COPY backend/tests ./tests

RUN dotnet restore Processa.slnx
RUN dotnet publish src/Processa.Api/Processa.Api.csproj -c Release -o /app/publish --no-restore

FROM mcr.microsoft.com/dotnet/aspnet:10.0-alpine AS runtime
WORKDIR /app

RUN addgroup -S processa && adduser -S processa -G processa
USER processa

COPY --from=build /app/publish .

EXPOSE 8080
ENTRYPOINT ["dotnet", "Processa.Api.dll"]
