#See https://aka.ms/containerfastmode to understand how Visual Studio uses this Dockerfile to build your images for faster debugging.

FROM mcr.microsoft.com/dotnet/aspnet:6.0 AS base
WORKDIR /app
EXPOSE 80
EXPOSE 443
ARG VERSION=0

FROM mcr.microsoft.com/dotnet/sdk:6.0 AS build
ARG VERSION=0
WORKDIR /src
COPY ["IdentityManager/IdentityManager.csproj", "IdentityManager/"]
COPY ["Authorization Handlers/BeyondAuth.Acl/BeyondAuth.Acl.csproj", "Authorization Handlers/BeyondAuth.Acl/"]
COPY ["Password Validators/BeyondAuth.PasswordValidators.Common/BeyondAuth.PasswordValidators.Common.csproj", "Password Validators/BeyondAuth.PasswordValidators.Common/"]
COPY ["Password Validators/BeyondAuth.PasswordValidators.Topology/BeyondAuth.PasswordValidators.Topology.csproj", "Password Validators/BeyondAuth.PasswordValidators.Topology/"]
COPY ["Password Validators/BlackstarSolar.AspNetCore.Identity.PwnedPasswords/BlackstarSolar.AspNetCore.Identity.PwnedPasswords.csproj", "Password Validators/BlackstarSolar.AspNetCore.Identity.PwnedPasswords/"]
COPY ["Password Validators/SharpPwned.NET/SharpPwned.NET.csproj", "Password Validators/SharpPwned.NET/"]
COPY ["Audit.NET.RavenDB/Audit.NET.RavenDB.csproj", "Audit.NET.RavenDB/"]
COPY ["Identity.Core/Identity.Core.csproj", "Identity.Core/"]
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