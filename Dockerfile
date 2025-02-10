# Build stage
FROM mcr.microsoft.com/dotnet/sdk:6.0 AS build
WORKDIR /src

# Copy the project file(s) first and restore dependencies (optimized caching)
COPY ElasticApmPoc/ElasticApmPoc.csproj ElasticApmPoc/
RUN dotnet restore ElasticApmPoc/ElasticApmPoc.csproj

# Copy the full source code and build
COPY . .
WORKDIR /src/ElasticApmPoc
RUN dotnet publish -c Release -o out

# Runtime stage
FROM mcr.microsoft.com/dotnet/aspnet:6.0
WORKDIR /app

# Copy built files from the build stage
COPY --from=build /src/ElasticApmPoc/out .

# # Install curl in the runtime container
RUN apt-get update && apt-get install -y curl && rm -rf /var/lib/apt/lists/*

# Set environment variables for Elastic APM
ENV ELASTIC_APM_SERVER_URL="http://apm-server:8200"
ENV ELASTIC_APM_SERVICE_NAME="ElasticApmPoc"
ENV ELASTIC_APM_ENVIRONMENT="production"

# Ensure app runs on the correct port
ENV ASPNETCORE_URLS="http://+:5000"

# Expose necessary ports
EXPOSE 5000

# Run the application
ENTRYPOINT ["dotnet", "ElasticApmPoc.dll"]

# # Build stage
# FROM mcr.microsoft.com/dotnet/sdk:6.0 AS build
# WORKDIR /src

# # Copy the project file(s) first and restore dependencies (optimized caching)
# COPY ElasticApmPoc/ElasticApmPoc.csproj ./ 
# RUN dotnet restore ElasticApmPoc.csproj

# # Copy the full source code and build
# COPY . .
# WORKDIR /src/ElasticApmPoc
# RUN dotnet publish -c Release -o out

# # Runtime stage
# FROM mcr.microsoft.com/dotnet/aspnet:6.0
# WORKDIR /app

# # Copy built files from the build stage
# COPY --from=build /src/ElasticApmPoc/out . 

# # Install curl in the runtime container
# RUN apt-get update && apt-get install -y curl && rm -rf /var/lib/apt/lists/*

# # Set environment variables for Elastic APM
# ENV ELASTIC_APM_SERVER_URL="http://apm-server:8200"
# ENV ELASTIC_APM_SERVICE_NAME="ElasticApmPoc"
# ENV ELASTIC_APM_ENVIRONMENT="production"

# # Ensure app runs on the correct port
# ENV ASPNETCORE_URLS="http://+:5000"

# # Expose necessary ports
# EXPOSE 5000

# # Run the application
# ENTRYPOINT ["dotnet", "ElasticApmPoc.dll"]
