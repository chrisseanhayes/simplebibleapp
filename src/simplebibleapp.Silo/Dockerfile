# 1. Use the .NET 8 SDK image for building
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build-env
WORKDIR /app

# Copy everything and publish
COPY . ./
RUN dotnet publish ./simplebibleapp.Silo/simplebibleapp.Silo.csproj -c Release -o /app/out

# 2. Use the ASP.NET Core runtime image (or Runtime image)
FROM mcr.microsoft.com/dotnet/aspnet:8.0
WORKDIR /app

# Install Antigravity CLI (agy) so the Silo can spawn it
RUN apt-get update && apt-get install -y curl ca-certificates --no-install-recommends \
    && curl -fsSL https://antigravity.google/cli/install.sh | bash \
    && apt-get clean && rm -rf /var/lib/apt/lists/*

# Copy the published output
COPY --from=build-env /app/out .

# Expose agy install location to PATH
ENV PATH="/root/.local/bin:${PATH}"

# Orleans Silo ports
EXPOSE 11111 30000

ENTRYPOINT ["dotnet", "simplebibleapp.Silo.dll"]
