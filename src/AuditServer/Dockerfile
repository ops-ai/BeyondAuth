FROM mcr.microsoft.com/dotnet/core/aspnet:3.1-buster-slim AS base
WORKDIR /app
EXPOSE 80
EXPOSE 443
ARG VERSION=0  

FROM mcr.microsoft.com/dotnet/core/sdk:3.1-buster AS build
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
