FROM mcr.microsoft.com/dotnet/core/aspnet:3.1-buster-slim AS base
WORKDIR /app
EXPOSE 80
EXPOSE 443
ARG VERSION=0

FROM mcr.microsoft.com/dotnet/core/sdk:3.1-buster AS build
ARG VERSION=0
WORKDIR /src
COPY ["PolicyServer/PolicyServer.csproj", "PolicyServer/"]
COPY ["BeyondAuth.PolicyServer.Core/BeyondAuth.PolicyServer.Core.csproj", "BeyondAuth.PolicyServer.Core/"]
RUN dotnet restore "PolicyServer/PolicyServer.csproj"
COPY . .
WORKDIR "/src/PolicyServer"
RUN dotnet build "PolicyServer.csproj" -c Release /p:Version=${VERSION} -o /app/build

FROM build AS publish
ARG VERSION=0
RUN dotnet publish "PolicyServer.csproj" -c Release /p:Version=${VERSION} -o /app/publish

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "PolicyServer.dll"]
