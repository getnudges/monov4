FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build

WORKDIR /src
COPY PaymentApi.Docker.sln ./
COPY Nudges.Data/*.csproj ./Nudges.Data/
COPY Nudges.Auth.Web/*.csproj ./Nudges.Auth.Web/
COPY PaymentApi/*.csproj ./PaymentApi/

RUN dotnet restore
COPY . .
WORKDIR /src/PaymentApi
RUN dotnet build -c Release -o /app

FROM build AS publish
RUN dotnet publish "PaymentApi.csproj" -c Release -o /src/publish /p:UseAppHost=false

FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS final
WORKDIR /app
COPY --from=publish /src/publish .
ENTRYPOINT ["dotnet", "PaymentApi.dll"]



