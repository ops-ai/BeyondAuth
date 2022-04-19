#See https://aka.ms/containerfastmode to understand how Visual Studio uses this Dockerfile to build your images for faster debugging.

FROM mcr.microsoft.com/dotnet/aspnet:6.0 AS base
WORKDIR /app
EXPOSE 80
EXPOSE 443
ARG VERSION=0

FROM mcr.microsoft.com/dotnet/sdk:6.0 AS build
ARG VERSION=0
WORKDIR /src
COPY ["AuthorizationServer/AuthorizationServer.csproj", "AuthorizationServer/"]
COPY ["Authorization Handlers/AuthorizationPolicyProvider/BeyondAuth.PolicyProvider.csproj", "Authorization Handlers/AuthorizationPolicyProvider/"]
COPY ["BeyondAuth.PolicyServer.Core/BeyondAuth.PolicyServer.Core.csproj", "BeyondAuth.PolicyServer.Core/"]
COPY ["Authorization Handlers/BeyondAuth.Acl/BeyondAuth.Acl.csproj", "Authorization Handlers/BeyondAuth.Acl/"]
COPY ["Authorization Handlers/BeyondAuth.RelatedDataValidation/BeyondAuth.RelatedDataValidation.csproj", "Authorization Handlers/BeyondAuth.RelatedDataValidation/"]
RUN dotnet restore "AuthorizationServer/AuthorizationServer.csproj"
COPY . .
WORKDIR "/src/AuthorizationServer"
RUN dotnet build "AuthorizationServer.csproj" -c Release /p:Version=${VERSION} -o /app/build

FROM build AS publish
ARG VERSION=0
RUN dotnet publish "AuthorizationServer.csproj" -c Release /p:Version=${VERSION} -o /app/publish

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "AuthorizationServer.dll"]
