FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build
WORKDIR /src
COPY BeeciyeMarket/BeeciyeMarket.csproj BeeciyeMarket/
RUN dotnet restore BeeciyeMarket/BeeciyeMarket.csproj
COPY BeeciyeMarket/ BeeciyeMarket/
RUN dotnet publish BeeciyeMarket/BeeciyeMarket.csproj -c Release -o /app/publish /p:UseAppHost=false

FROM mcr.microsoft.com/dotnet/aspnet:9.0
WORKDIR /app
COPY --from=build /app/publish .
ENV ASPNETCORE_ENVIRONMENT=Production
# Railway provides PORT at runtime
CMD ["sh", "-c", "ASPNETCORE_URLS=http://0.0.0.0:${PORT:-8080} dotnet BeeciyeMarket.dll"]
