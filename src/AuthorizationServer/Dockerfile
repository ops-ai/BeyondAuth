FROM mcr.microsoft.com/dotnet/core/aspnet:3.1-buster-slim AS base
WORKDIR /app
EXPOSE 80
EXPOSE 443

FROM mcr.microsoft.com/dotnet/core/sdk:3.1-buster AS build
WORKDIR /src
COPY ["AuthorizationServer/AuthorizationServer.csproj", "AuthorizationServer/"]
RUN dotnet restore "AuthorizationServer/AuthorizationServer.csproj"
COPY . .
WORKDIR "/src/AuthorizationServer"
RUN dotnet build "AuthorizationServer.csproj" -c Release -o /app/build

FROM build AS publish
RUN dotnet publish "AuthorizationServer.csproj" -c Release -o /app/publish

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "AuthorizationServer.dll"]