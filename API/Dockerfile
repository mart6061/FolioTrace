FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

COPY FolioTrace.slnx ./
COPY API/API.csproj API/
COPY Features/Features.csproj Features/
COPY Repository/Repository.csproj Repository/
RUN dotnet restore API/API.csproj

COPY . .
RUN dotnet publish API/API.csproj --configuration Release --output /app/publish --no-restore

FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS runtime
WORKDIR /app
COPY --from=build /app/publish .

ENV ASPNETCORE_ENVIRONMENT=Production
ENTRYPOINT ["sh", "-c", "dotnet API.dll --urls http://0.0.0.0:${PORT:-8080}"]
