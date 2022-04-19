#See https://aka.ms/containerfastmode to understand how Visual Studio uses this Dockerfile to build your images for faster debugging.

FROM mcr.microsoft.com/dotnet/aspnet:6.0 AS base
WORKDIR /app
EXPOSE 80
EXPOSE 443
ARG VERSION=0

FROM mcr.microsoft.com/dotnet/sdk:6.0 AS build
ARG VERSION=0
WORKDIR /src
COPY ["Authentication/Authentication.csproj", "Authentication/"]
COPY ["IdentityServer4.Contrib.RavenDB/IdentityServer4.Contrib.RavenDB.csproj", "IdentityServer4.Contrib.RavenDB/"]
RUN dotnet restore "Authentication/Authentication.csproj"
COPY . .
WORKDIR "/src/Authentication"
RUN dotnet build "Authentication.csproj" -c Release /p:Version=${VERSION} -o /app/build

FROM build AS publish
ARG VERSION=0
RUN dotnet publish "Authentication.csproj" -c Release /p:Version=${VERSION} -o /app/publish

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "Authentication.dll"]
