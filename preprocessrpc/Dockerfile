# Etapa de build
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

COPY preprocessrpc.csproj ./
RUN dotnet restore

COPY . ./
RUN dotnet publish -c Release -o /out

# Etapa de runtime
FROM mcr.microsoft.com/dotnet/aspnet:8.0
WORKDIR /app
COPY --from=build /out .

# Expor a porta usada no Kestrel configurado com HTTP/2
EXPOSE 8080

ENTRYPOINT ["dotnet", "preprocessrpc.dll"]
