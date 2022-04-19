FROM mcr.microsoft.com/dotnet/aspnet:6.0 AS base
WORKDIR /app
EXPOSE 80
EXPOSE 443
ARG VERSION=0

FROM mcr.microsoft.com/dotnet/sdk:6.0 AS build
ARG VERSION=0
WORKDIR /src
COPY ["PolicyServer/PolicyServer.csproj", "PolicyServer/"]
COPY ["PolicyServer.Core/BeyondAuth.PolicyServer.Core.csproj", "PolicyServer.Core/"]
COPY ["Identity.Core/Identity.Core.csproj", "Identity.Core/"]
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
