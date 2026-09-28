# src/Frontend.ConexaoSolidaria/Dockerfile
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

COPY ConexaoSolidaria.sln ./
COPY src/Frontend.ConexaoSolidaria/Frontend.ConexaoSolidaria.csproj src/Frontend.ConexaoSolidaria/
RUN dotnet restore src/Frontend.ConexaoSolidaria/Frontend.ConexaoSolidaria.csproj

COPY src/Frontend.ConexaoSolidaria/ src/Frontend.ConexaoSolidaria/
RUN dotnet publish src/Frontend.ConexaoSolidaria/Frontend.ConexaoSolidaria.csproj -c Release -o /app/publish --no-restore

FROM nginx:1.27-alpine AS final
COPY src/Frontend.ConexaoSolidaria/nginx.conf /etc/nginx/conf.d/default.conf
COPY --from=build /app/publish/wwwroot /usr/share/nginx/html
EXPOSE 8080
