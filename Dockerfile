# Build step
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

# Copy solution and project files
COPY *.sln ./
COPY LabAssistantOPP_LAO.WebApi/*.csproj ./LabAssistantOPP_LAO.WebApi/
COPY LabAssistantOPP_LAO.Services/*.csproj ./LabAssistantOPP_LAO.Services/
COPY LabAssistantOPP_LAO.DTO/*.csproj ./LabAssistantOPP_LAO.DTO/
COPY LabAssistantOPP_LAO.Models/*.csproj ./LabAssistantOPP_LAO.Models/
COPY NewGradingTest/*.csproj ./NewGradingTest/

# Restore NuGet packages
RUN dotnet restore

# Copy source code
COPY . .

# Build and publish
RUN dotnet publish LabAssistantOPP_LAO.WebApi/LabAssistantOPP_LAO.WebApi.csproj -c Release -o /app/publish

# Runtime step
FROM mcr.microsoft.com/dotnet/aspnet:8.0
WORKDIR /app
COPY --from=build /app/publish .

# Set environment variables (optional for App Platform)
ENV ASPNETCORE_URLS=http://+:8080 \
    DOTNET_RUNNING_IN_CONTAINER=true \
    DOTNET_SYSTEM_GLOBALIZATION_INVARIANT=false

# Expose port (important for App Platform)
EXPOSE 8080

# Start app
ENTRYPOINT ["dotnet", "LabAssistantOPP_LAO.WebApi.dll"]
