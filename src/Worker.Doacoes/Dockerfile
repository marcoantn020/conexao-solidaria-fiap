# src/Worker.Doacoes/Dockerfile
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

COPY ConexaoSolidaria.sln ./
COPY src/Worker.Doacoes/Worker.Doacoes.csproj src/Worker.Doacoes/
COPY tests/Worker.Doacoes.Tests/Worker.Doacoes.Tests.csproj tests/Worker.Doacoes.Tests/
RUN dotnet restore src/Worker.Doacoes/Worker.Doacoes.csproj

COPY src/Worker.Doacoes/ src/Worker.Doacoes/
RUN dotnet publish src/Worker.Doacoes/Worker.Doacoes.csproj -c Release -o /app/publish --no-restore

FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS final
WORKDIR /app
ENV ASPNETCORE_URLS=http://+:8080
EXPOSE 8080
COPY --from=build /app/publish .
ENTRYPOINT ["dotnet", "Worker.Doacoes.dll"]
