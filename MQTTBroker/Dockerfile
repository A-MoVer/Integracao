# Use a imagem base do SDK .NET para build e execução
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build-env

# Configure o diretório de trabalho dentro do contêiner
WORKDIR /app

# Copie o arquivo de projeto e restaure as dependências
COPY *.csproj ./
RUN dotnet restore

# Copie todo o código fonte para o contêiner
COPY . ./

# Compile o projeto
RUN dotnet build 

# Segunda etapa: use a imagem runtime para execução
FROM mcr.microsoft.com/dotnet/runtime:8.0

# Configure o diretório de trabalho
WORKDIR /app

# Copie os binários compilados para a nova imagem
COPY --from=build-env /app .

# Comando padrão para executar o aplicativo
ENTRYPOINT ["dotnet", "/app/bin/Debug/net8.0/MQTTBroker.dll"]
