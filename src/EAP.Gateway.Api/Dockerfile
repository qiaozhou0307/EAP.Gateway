// Dockerfile
FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS base
WORKDIR /app
EXPOSE 80
EXPOSE 443

FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src
COPY ["src/EAP.Gateway.Api/EAP.Gateway.Api.csproj", "src/EAP.Gateway.Api/"]
COPY ["src/EAP.Gateway.Application/EAP.Gateway.Application.csproj", "src/EAP.Gateway.Application/"]
COPY ["src/EAP.Gateway.Infrastructure/EAP.Gateway.Infrastructure.csproj", "src/EAP.Gateway.Infrastructure/"]
COPY ["src/EAP.Gateway.Core/EAP.Gateway.Core.csproj", "src/EAP.Gateway.Core/"]
COPY ["Directory.Packages.props", "."]

RUN dotnet restore "src/EAP.Gateway.Api/EAP.Gateway.Api.csproj"
COPY . .
WORKDIR "/src/src/EAP.Gateway.Api"
RUN dotnet build "EAP.Gateway.Api.csproj" -c Release -o /app/build

FROM build AS publish
RUN dotnet publish "EAP.Gateway.Api.csproj" -c Release -o /app/publish /p:UseAppHost=false

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "EAP.Gateway.Api.dll"]
