FROM microsoft/aspnetcore:2.0 AS base
WORKDIR /app
EXPOSE 80

FROM microsoft/aspnetcore-build:2.0 AS build
WORKDIR /src
COPY Whalerator.sln ./
COPY Whalerator.WebAPI/Whalerator.WebAPI.csproj Whalerator.WebAPI/
COPY Whalerator/Whalerator.csproj Whalerator/
COPY Whalerator.Support/Whalerator.Support.csproj Whalerator.Support/
RUN dotnet restore -nowarn:msb3202,nu1503
COPY . .
WORKDIR /src/Whalerator.WebAPI
RUN dotnet build -c Release -o /app

FROM build AS publish
RUN dotnet publish -c Release -o /app

FROM base AS final
WORKDIR /app
COPY --from=publish /app .
ENV layercache="/var/data"
ENTRYPOINT ["dotnet", "Whalerator.WebAPI.dll"]
