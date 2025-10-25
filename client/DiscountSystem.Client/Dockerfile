# Build stage
FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

COPY DiscountSystem.slnx ./
COPY src/DiscountSystem.Protos/ ./DiscountSystem.Protos/
COPY client/DiscountSystem.Client/ ./DiscountSystem.Client/

RUN dotnet restore DiscountSystem.Client/DiscountSystem.Client.csproj
RUN dotnet publish DiscountSystem.Client/DiscountSystem.Client.csproj -c Release -o /app/publish

# Runtime stage
FROM mcr.microsoft.com/dotnet/runtime:10.0
WORKDIR /app

# Copy published files
COPY --from=build /app/publish .

# Copy mounted HTTPS certificate (optional, if client validation required)
VOLUME ["/https"]

# Allow client to talk to HTTPS gRPC server
ENV GRPC_SERVER_URL=https://server:5001
ENV DOTNET_SYSTEM_NET_HTTP_USESOCKETSHTTPHANDLER=true

ENTRYPOINT ["dotnet", "DiscountSystem.Client.dll"]
