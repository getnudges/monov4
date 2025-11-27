FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build

WORKDIR /src
COPY Nudges.AuthInit.slnx ./
COPY Nudges.AuthInit/*.csproj ./Nudges.AuthInit/
COPY Nudges.Auth.Keycloak/*.csproj ./Nudges.Auth.Keycloak/
COPY Nudges.Auth/*.csproj ./Nudges.Auth/
COPY Nudges.Data/*.csproj ./Nudges.Data/
COPY Nudges.Data.Security/*.csproj ./Nudges.Data.Security/
COPY Nudges.Security/*.csproj ./Nudges.Security/
COPY Monads/*.csproj ./Monads/
COPY Precision.WarpCache/Precision.WarpCache/*.csproj ./Precision.WarpCache/Precision.WarpCache/
COPY Nudges.Configuration.Analyzers/*.csproj ./Nudges.Configuration.Analyzers/

RUN dotnet restore Nudges.AuthInit.slnx

COPY Nudges.AuthInit/ ./Nudges.AuthInit/
COPY Nudges.Auth.Keycloak/ ./Nudges.Auth.Keycloak/
COPY Nudges.Auth/ ./Nudges.Auth/
COPY Nudges.Data/ ./Nudges.Data/
COPY Nudges.Data.Security/ ./Nudges.Data.Security/
COPY Monads/ ./Monads/
COPY Nudges.Security/ ./Nudges.Security/
COPY Precision.WarpCache/Precision.WarpCache/ ./Precision.WarpCache/Precision.WarpCache/
COPY Nudges.Configuration.Analyzers/ ./Nudges.Configuration.Analyzers/

FROM build AS publish

WORKDIR /src/Nudges.AuthInit
RUN dotnet publish Nudges.AuthInit.csproj -c Release -o /src/publish /p:UseAppHost=true

FROM mcr.microsoft.com/dotnet/runtime:10.0 AS final
WORKDIR /app
COPY --from=publish /src/publish .

ENTRYPOINT [ "dotnet", "Nudges.AuthInit.dll", "seed" ]
