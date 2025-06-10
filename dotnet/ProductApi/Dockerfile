FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build

WORKDIR /src
COPY ProductApi.sln ./
COPY UnAd.Data/*.csproj ./UnAd.Data/
COPY UnAd.Auth.Web/*.csproj ./UnAd.Auth.Web/
COPY ProductApi/*.csproj ./ProductApi/

RUN dotnet restore
COPY . .
WORKDIR /src/ProductApi
RUN dotnet build -c Release -o /app

FROM build AS publish
RUN dotnet publish "ProductApi.csproj" -c Release -o /src/publish /p:UseAppHost=false

FROM mcr.microsoft.com/dotnet/aspnet:9.0 AS final
WORKDIR /app
COPY --from=publish /src/publish .
ENTRYPOINT ["dotnet", "ProductApi.dll"]



