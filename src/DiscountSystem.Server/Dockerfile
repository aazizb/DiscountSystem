# Build stage
FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

COPY DiscountSystem.slnx ./
COPY src/DiscountSystem.Domain/ ./DiscountSystem.Domain/
COPY src/DiscountSystem.Infrastructure/ ./DiscountSystem.Infrastructure/
COPY src/DiscountSystem.Protos/ ./DiscountSystem.Protos/
COPY src/DiscountSystem.Services/ ./DiscountSystem.Services/
COPY src/DiscountSystem.Server/ ./DiscountSystem.Server/

RUN dotnet restore DiscountSystem.Server/DiscountSystem.Server.csproj
RUN dotnet publish DiscountSystem.Server/DiscountSystem.Server.csproj -c Release -o /app/publish

# Runtime image
FROM mcr.microsoft.com/dotnet/aspnet:10.0
WORKDIR /app

# Copy published files
COPY --from=build /app/publish .

# HTTP/3 + HTTPS setup
EXPOSE 5001
ENV ASPNETCORE_URLS=https://+:5001
ENV ASPNETCORE_Kestrel__Certificates__Default__Path=/https/aspnetapp.pfx
ENV ASPNETCORE_Kestrel__Certificates__Default__Password=Uddito.Damal03

# Certificate mounted from host (see docker-compose)
ENTRYPOINT ["dotnet", "DiscountSystem.Server.dll"]
