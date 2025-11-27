FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build

WORKDIR /src
COPY DbSeeder.sln ./
COPY tools/DbSeeder/*.csproj ./tools/DbSeeder/
COPY Nudges.Redis/*.csproj ./Nudges.Redis/
COPY Nudges.Data/*.csproj ./Nudges.Data/
COPY Nudges.Security/*.csproj ./Nudges.Security/
COPY Nudges.Data.Security/*.csproj ./Nudges.Data.Security/

RUN dotnet restore

COPY tools/DbSeeder/ ./tools/DbSeeder/
COPY Nudges.Redis/ ./Nudges.Redis/
COPY Nudges.Data/ ./Nudges.Data/
COPY Nudges.Security/ ./Nudges.Security/
COPY Nudges.Data.Security/ ./Nudges.Data.Security/

FROM build AS publish

WORKDIR /src/tools/DbSeeder
RUN dotnet publish DbSeeder.csproj -c Release -o /src/publish /p:UseAppHost=true

FROM mcr.microsoft.com/dotnet/runtime:10.0 AS final
WORKDIR /app
COPY --from=publish /src/publish .

ENTRYPOINT [ "/app/seed", "db" ]
