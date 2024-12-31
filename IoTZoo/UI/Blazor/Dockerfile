#See https://aka.ms/customizecontainer to learn how to customize your debug container and how Visual Studio uses this Dockerfile to build your images for faster debugging.


FROM mcr.microsoft.com/dotnet/aspnet:9.0 AS base
WORKDIR /app
EXPOSE 8080
EXPOSE 8081

# https://blog.devgenius.io/net-core-tip-1-beware-of-timezones-across-platforms-like-windows-os-and-linux-docker-containers-9fd3287b1e16
RUN apt-get update && apt-get install -y tzdata
ENV TZ="Europe/Berlin"


FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build
ARG BUILD_CONFIGURATION=Release
WORKDIR /src

COPY ["UI/Blazor/IotZoo.csproj", "UI/Blazor/"]
COPY ["Domain/Domain.csproj", "DataAccess/"]
COPY ["Infrastructure/Infrastructure.csproj", "Infrastructure/"]
COPY ["SunriseCalculator/SunriseAndSunset.csproj", "SunriseCalculator/"]
RUN dotnet restore "./UI/Blazor/./IotZoo.csproj"
COPY . .
WORKDIR "/src/UI/Blazor"
RUN dotnet build "./IotZoo.csproj" -c $BUILD_CONFIGURATION -o /app/build

FROM build AS publish
ARG BUILD_CONFIGURATION=Release
RUN dotnet publish "./IotZoo.csproj" -c $BUILD_CONFIGURATION -o /app/publish /p:UseAppHost=false

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "IotZoo.dll"]