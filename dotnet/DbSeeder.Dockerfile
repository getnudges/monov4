ARG ConnectionStrings__UserDb

FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build

WORKDIR /src
COPY DbSeeder.sln ./
COPY tools/DbSeeder/*.csproj ./tools/DbSeeder/
COPY UnAd.Redis/*.csproj ./UnAd.Redis/
COPY UnAd.Data/*.csproj ./UnAd.Data/

RUN dotnet restore

COPY tools/DbSeeder/ ./tools/DbSeeder/
COPY UnAd.Redis/ ./UnAd.Redis/
COPY UnAd.Data/ ./UnAd.Data/

FROM build AS publish

WORKDIR /src/tools/DbSeeder
RUN dotnet publish DbSeeder.csproj -c Release -o /src/publish /p:UseAppHost=true

FROM mcr.microsoft.com/dotnet/runtime:9.0 AS final
WORKDIR /app
COPY --from=publish /src/publish .

ENV ConnectionStrings__UserDb="$ConnectionStrings__UserDb"

CMD ./seed db



