FROM mcr.microsoft.com/dotnet/core/aspnet:3.1-buster-slim AS base
WORKDIR /app
EXPOSE 80
EXPOSE 443

RUN apt-get update -y
RUN curl -sL https://deb.nodesource.com/setup_10.x | bash --debug
RUN apt-get install nodejs -yq

FROM mcr.microsoft.com/dotnet/core/sdk:3.1-buster AS build
WORKDIR /src
COPY ["BeyondAuth.Web/BeyondAuth.Web.csproj", "BeyondAuth.Web/"]

# Setup NodeJs
RUN apt-get update -y
RUN curl -sL https://deb.nodesource.com/setup_10.x | bash --debug
RUN apt-get install nodejs -yq

#RUN apt-get install nodejs-dev libnode-dev node-gyp build-essential  -yq

RUN dotnet restore "BeyondAuth.Web/BeyondAuth.Web.csproj"
COPY . .
WORKDIR "/src/BeyondAuth.Web"
#RUN dotnet build "BeyondAuth.Web.csproj" -c Release -o /app/build

FROM build AS publish
RUN dotnet publish "BeyondAuth.Web.csproj" -c Release -o /app/publish

FROM base AS final
WORKDIR /app

COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "BeyondAuth.Web.dll"]