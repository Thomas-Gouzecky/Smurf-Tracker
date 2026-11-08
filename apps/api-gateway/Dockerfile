# Build stage
FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build

WORKDIR /src

COPY . .

RUN dotnet restore

RUN dotnet publish -c Release -o /app/publish


# Runtime stage
FROM mcr.microsoft.com/dotnet/aspnet:9.0 AS runtime

WORKDIR /app

RUN apt-get update && apt-get install -y curl && rm -rf /var/lib/apt/lists/*

COPY secrets/dev-ca.crt /usr/local/share/ca-certificates/dev-ca.crt

RUN update-ca-certificates

COPY --from=build /app/publish .

EXPOSE 8008

ENTRYPOINT ["dotnet", "ApiGateway.dll"]