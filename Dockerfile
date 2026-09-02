# Use the official .NET 8 SDK image for building
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

# Copy csproj and restore dependencies
COPY ["InventoryZeroAPI.csproj", "."]
RUN dotnet restore "./InventoryZeroAPI.csproj"

# Copy everything else and build
COPY . .
WORKDIR "/src/."
RUN dotnet build "InventoryZeroAPI.csproj" -c Release -o /app/build

# Publish the application
FROM build AS publish
RUN dotnet publish "InventoryZeroAPI.csproj" -c Release -o /app/publish /p:UseAppHost=false

# Build the final runtime image
FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS final
WORKDIR /app

# Copy published files
COPY --from=publish /app/publish .

# Create directory for logs
RUN mkdir -p /app/logs

# Expose port 8080 (Railway default)
EXPOSE 8080

# Set environment variable for port
ENV ASPNETCORE_URLS=http://+:8080

# Run the application
ENTRYPOINT ["dotnet", "InventoryZeroAPI.dll"]