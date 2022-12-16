#See https://aka.ms/containerfastmode to understand how Visual Studio uses this Dockerfile to build your images for faster debugging.

FROM mcr.microsoft.com/dotnet/aspnet:7.0 AS base
WORKDIR /app
EXPOSE 80
EXPOSE 443
ARG VERSION=0

FROM mcr.microsoft.com/dotnet/sdk:7.0 AS build
ARG VERSION=0
WORKDIR /src
COPY ["Documentation/Documentation.csproj", "Documentation/"]
RUN dotnet restore "Documentation/Documentation.csproj"
COPY . .
WORKDIR "/src/Documentation"
RUN dotnet build "Documentation.csproj" -c Release /p:Version=${VERSION} -o /app/build

FROM build AS publish
ARG VERSION=0
RUN dotnet publish "Documentation.csproj" -c Release /p:Version=${VERSION} -o /app/publish

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "Documentation.dll"]