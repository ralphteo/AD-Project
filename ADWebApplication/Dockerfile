# Stage 1 - Build
FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /app

# Copy csproj and restore for caching
COPY *.csproj ./
RUN dotnet restore

# Copy everything else and publish
COPY . ./
RUN dotnet publish -c Release -o out

# Stage 2 - Runtime
FROM mcr.microsoft.com/dotnet/aspnet:10.0
WORKDIR /app

# Install adduser so we can create a non-root user
RUN apt-get update && \
    apt-get install -y --no-install-recommends adduser passwd && \
    rm -rf /var/lib/apt/lists/*

# Create a non-root user
RUN addgroup --system appgroup && adduser --system --ingroup appgroup appuser
USER appuser

# Copy published output from build stage
COPY --from=build /app/out ./

# Expose container port
EXPOSE 8080

# Healthcheck for container
HEALTHCHECK --interval=30s --timeout=5s --start-period=10s --retries=3 \
  CMD curl -f http://localhost:8080/health || exit 1

# Run the app
ENTRYPOINT ["dotnet", "ADWebApplication.dll"]
