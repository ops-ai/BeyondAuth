#See https://aka.ms/containerfastmode to understand how Visual Studio uses this Dockerfile to build your images for faster debugging.

FROM mcr.microsoft.com/dotnet/core/aspnet:3.1-buster-slim AS base
WORKDIR /app
EXPOSE 80
EXPOSE 443
ARG VERSION=0

FROM mcr.microsoft.com/dotnet/core/sdk:3.1-buster AS build
ARG VERSION=0
WORKDIR /src
COPY ["IdentityManager/IdentityManager.csproj", "IdentityManager/"]
RUN dotnet restore "IdentityManager/IdentityManager.csproj"
COPY . .
WORKDIR "/src/IdentityManager"
RUN dotnet build "IdentityManager.csproj" -c Release /p:Version=${VERSION} -o /app/build

FROM build AS publish
ARG VERSION=0
RUN dotnet publish "IdentityManager.csproj" -c Release /p:Version=${VERSION} -o /app/publish

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "IdentityManager.dll"]