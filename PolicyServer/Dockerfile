FROM mcr.microsoft.com/dotnet/core/aspnet:3.1-buster-slim AS base
WORKDIR /app
EXPOSE 80
EXPOSE 443

FROM mcr.microsoft.com/dotnet/core/sdk:3.1-buster AS build
WORKDIR /src
COPY ["PolicyServer/PolicyServer.csproj", "PolicyServer/"]
RUN dotnet restore "PolicyServer/PolicyServer.csproj"
COPY . .
WORKDIR "/src/PolicyServer"
RUN dotnet build "PolicyServer.csproj" -c Release -o /app/build

FROM build AS publish
RUN dotnet publish "PolicyServer.csproj" -c Release -o /app/publish

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "PolicyServer.dll"]