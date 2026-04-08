FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

COPY SmppClient.sln .
COPY src/SmppClient/SmppClient.csproj src/SmppClient/
COPY src/SmppStorage/SmppStorage.csproj src/SmppStorage/
COPY src/SmppGateway/SmppGateway.csproj src/SmppGateway/

RUN dotnet restore
COPY . .
RUN dotnet publish src/SmppGateway/SmppGateway.csproj -c Release -o /app/publish

FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS runtime
WORKDIR /app
COPY --from=build /app/publish .

HEALTHCHECK --interval=30s --timeout=3s --start-period=5s --retries=3 \
  CMD curl -f http://localhost:8080/healthz || exit 1

EXPOSE 8080
ENTRYPOINT ["dotnet", "SmppGateway.dll"]
