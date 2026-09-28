# src/Api.CampanhasUsuarios/Dockerfile
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

COPY ConexaoSolidaria.sln ./
COPY src/Api.CampanhasUsuarios/Api.CampanhasUsuarios.csproj src/Api.CampanhasUsuarios/
COPY tests/Api.CampanhasUsuarios.Tests/Api.CampanhasUsuarios.Tests.csproj tests/Api.CampanhasUsuarios.Tests/
RUN dotnet restore src/Api.CampanhasUsuarios/Api.CampanhasUsuarios.csproj

COPY src/Api.CampanhasUsuarios/ src/Api.CampanhasUsuarios/
RUN dotnet publish src/Api.CampanhasUsuarios/Api.CampanhasUsuarios.csproj -c Release -o /app/publish --no-restore

FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS final
WORKDIR /app
ENV ASPNETCORE_URLS=http://+:8080
EXPOSE 8080
COPY --from=build /app/publish .
ENTRYPOINT ["dotnet", "Api.CampanhasUsuarios.dll"]
