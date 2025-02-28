# Stage 1: Build
FROM --platform=$BUILDPLATFORM mcr.microsoft.com/dotnet/sdk:8.0 AS build

WORKDIR /app

# Copiar apenas o arquivo .csproj e restaurar dependências
COPY *.csproj ./
RUN dotnet restore

# Copiar o restante do código e publicar a aplicação
COPY . ./
RUN dotnet publish --configuration Release --output out

# Stage 2: Runtime
FROM --platform=$TARGETPLATFORM mcr.microsoft.com/dotnet/runtime:8.0

WORKDIR /app

# Copiar os artefatos publicados da etapa de build
COPY --from=build /app/out ./

# Definir o entrypoint para executar a aplicação
ENTRYPOINT ["dotnet", "MqttConsoleClient.dll"]
