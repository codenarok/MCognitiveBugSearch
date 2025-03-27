# Build stage
FROM mcr.microsoft.com/dotnet/sdk:6.0 AS build
WORKDIR /src

# Copy the project file and restore
COPY api/*.csproj ./api/
RUN dotnet restore ./api/my-functions-app.csproj

# Copy the entire function app code
COPY api/. ./api/
WORKDIR /src/api

# Build and publish the function app
RUN dotnet publish -c Release -o /app/publish

# Runtime stage
FROM mcr.microsoft.com/azure-functions/dotnet-isolated:4

# ✅ Install Azure CLI (optional for local Azure CLI auth inside container)
RUN apt-get update && \
    apt-get install -y curl apt-transport-https lsb-release gnupg && \
    curl -sL https://packages.microsoft.com/keys/microsoft.asc | gpg --dearmor > microsoft.gpg && \
    install -o root -g root -m 644 microsoft.gpg /usr/share/keyrings/ && \
    echo "deb [arch=amd64 signed-by=/usr/share/keyrings/microsoft.gpg] https://packages.microsoft.com/repos/azure-cli/ $(lsb_release -cs) main" > /etc/apt/sources.list.d/azure-cli.list && \
    apt-get update && \
    apt-get install -y azure-cli && \
    rm -f microsoft.gpg

# Set working directory for the function runtime
WORKDIR /home/site/wwwroot

# ✅ Copy built app from publish step
COPY --from=build /app/publish .

# ✅ Environment settings
ENV AzureFunctionsJobHost__Logging__Console__IsEnabled=true
ENV FUNCTIONS_WORKER_RUNTIME=dotnet-isolated
