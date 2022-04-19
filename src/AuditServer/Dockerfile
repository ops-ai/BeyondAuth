FROM mcr.microsoft.com/dotnet/aspnet:6.0 AS base
WORKDIR /app
EXPOSE 80
EXPOSE 443
ARG VERSION=0  

FROM mcr.microsoft.com/dotnet/sdk:6.0 AS build
ARG VERSION=0
WORKDIR /src
COPY ["AuditServer/AuditServer.csproj", "AuditServer/"]
COPY ["Blockchain/Blockchain.csproj", "Blockchain/"]
COPY ["Cryptography/Cryptography.csproj", "Cryptography/"]
RUN dotnet restore "AuditServer/AuditServer.csproj"
COPY . .
WORKDIR "/src/AuditServer"
RUN dotnet build "AuditServer.csproj" -c Release /p:Version=${VERSION} -o /app/build

FROM build AS publish
ARG VERSION=0
RUN dotnet publish "AuditServer.csproj" -c Release /p:Version=${VERSION} -o /app/publish

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "AuditServer.dll"]
