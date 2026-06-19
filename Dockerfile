FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build
WORKDIR /src

COPY GeorgiaERP.Domain/GeorgiaERP.Domain.csproj GeorgiaERP.Domain/
COPY GeorgiaERP.Application/GeorgiaERP.Application.csproj GeorgiaERP.Application/
COPY GeorgiaERP.Infrastructure/GeorgiaERP.Infrastructure.csproj GeorgiaERP.Infrastructure/
COPY GeorgiaERP.Api/GeorgiaERP.Api.csproj GeorgiaERP.Api/
RUN dotnet restore GeorgiaERP.Api/GeorgiaERP.Api.csproj

COPY . .
RUN dotnet publish GeorgiaERP.Api/GeorgiaERP.Api.csproj -c Release -o /app/publish --no-restore

FROM mcr.microsoft.com/dotnet/aspnet:9.0
WORKDIR /app
COPY --from=build /app/publish .
EXPOSE 5000
ENTRYPOINT ["dotnet", "GeorgiaERP.Api.dll"]
