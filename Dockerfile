# ----------------------
# Build stage
# ----------------------
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

# Copy solution và project files
COPY LabAssistantOPP_LAO.WebApi/ LabAssistantOPP_LAO.WebApi/
COPY LabAssistantOPP_LAO.Services/ LabAssistantOPP_LAO.Services/
COPY LabAssistantOPP_LAO.DTO/ LabAssistantOPP_LAO.DTO/
COPY LabAssistantOPP_LAO.Models/ LabAssistantOPP_LAO.Models/
COPY *.sln ./

RUN dotnet restore

# Copy toàn bộ source code
COPY . .

# Build & publish
RUN dotnet publish LabAssistantOPP_LAO.WebApi/LabAssistantOPP_LAO.WebApi.csproj -c Release -o /app/publish


# ----------------------
# Runtime stage
# ----------------------
FROM mcr.microsoft.com/dotnet/aspnet:8.0
WORKDIR /app

RUN apt-get update && \
    apt-get install -y --no-install-recommends openjdk-17-jdk unzip findutils time && \
    rm -rf /var/lib/apt/lists/*

# Copy app từ build stage
COPY --from=build /app/publish .

# Env .NET
ENV ASPNETCORE_URLS=http://+:8080 \
    DOTNET_RUNNING_IN_CONTAINER=true \
    DOTNET_SYSTEM_GLOBALIZATION_INVARIANT=false

EXPOSE 8080

ENTRYPOINT ["dotnet", "LabAssistantOPP_LAO.WebApi.dll"]
