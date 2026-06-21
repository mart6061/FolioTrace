FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

COPY FolioTrace.slnx ./
COPY FoleoTrader/FoleoTrader.csproj FoleoTrader/
RUN dotnet restore FoleoTrader/FoleoTrader.csproj

COPY . .
RUN dotnet publish FoleoTrader/FoleoTrader.csproj --configuration Release --output /app/publish --no-restore

FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS runtime
WORKDIR /app

ENV ASPNETCORE_ENVIRONMENT=Production
EXPOSE 8080
EXPOSE 9878

COPY --from=build /app/publish .

ENTRYPOINT ["sh", "-c", "dotnet FoleoTrader.dll --urls http://0.0.0.0:${PORT:-8080}"]
