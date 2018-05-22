FROM microsoft/aspnetcore:2.0 AS base
WORKDIR /app
EXPOSE 80

FROM microsoft/aspnetcore-build:2.0 AS build
WORKDIR /src
COPY lib/Whalerator.sln ./
COPY lib/Whalerator.WebAPI/Whalerator.WebAPI.csproj Whalerator.WebAPI/
COPY lib/Whalerator/Whalerator.csproj Whalerator/
COPY lib/Whalerator.Support/Whalerator.Support.csproj Whalerator.Support/
RUN dotnet restore -nowarn:msb3202,nu1503
COPY lib .
WORKDIR /src/Whalerator.WebAPI
RUN dotnet build -c Release -o /app

FROM build AS publish
RUN dotnet publish -c Release -o /app

FROM node:carbon as ngbuild
COPY web/src /web/src
COPY web/package.json /web/
COPY web/package-lock.json /web/
COPY web/angular.json /web/
COPY web/tsconfig.json /web/
WORKDIR /web
RUN npm install
RUN npm install @angular/cli
RUN /web/node_modules/@angular/cli/bin/ng build --prod --output-path /dist

FROM base AS final
WORKDIR /app
COPY --from=publish /app .
COPY --from=ngbuild /dist ./wwwroot
COPY lib/Whalerator.WebAPI/config-docker.yaml config.yaml

ARG SRC_HASH="Unknown"
ARG RELEASE="0.0"
RUN echo "{ \""hash\"": \""$SRC_HASH\"", \""release\"": \""$RELEASE\"" }" > ./wwwroot/assets/v.json
COPY docker-readme.md /README.md
ENTRYPOINT ["dotnet", "Whalerator.WebAPI.dll"]
