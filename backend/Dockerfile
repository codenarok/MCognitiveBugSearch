FROM mcr.microsoft.com/azure-functions/dotnet-isolated:4.0

WORKDIR /home/site/wwwroot
COPY . .

RUN apt-get update && apt-get install -y --no-install-recommends \
    ca-certificates && \
    rm -rf /var/lib/apt/lists/*

CMD [""dotnet"", ""Azure.Functions.Worker.dll""]
