# Build stage
FROM mcr.microsoft.com/dotnet/sdk:3.1 AS builder

WORKDIR /src

# Copy project files
COPY PKMVP-BE/Pkmvp.Api/Pkmvp.Api.csproj ./

# Restore dependencies
RUN dotnet restore "Pkmvp.Api.csproj"

# Copy source code
COPY PKMVP-BE/Pkmvp.Api/ .

# Build and publish
RUN dotnet publish "Pkmvp.Api.csproj" -c Release -o /app/publish

# Runtime stage
FROM mcr.microsoft.com/dotnet/aspnet:3.1

WORKDIR /app

# Copy published app from builder
COPY --from=builder /app/publish .

# Expose port
EXPOSE 80
EXPOSE 443

# Start application
ENTRYPOINT ["dotnet", "Pkmvp.Api.dll"]
