# 1. Use the .NET 8 SDK image for building
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build-env
WORKDIR /app

# Copy everything and publish
COPY . ./
RUN dotnet publish ./simplebibleapp/simplebibleapp.csproj -c Release -o /app/out

# 2. Use the ASP.NET Core runtime image
FROM mcr.microsoft.com/dotnet/aspnet:8.0
WORKDIR /app

# Install Antigravity CLI (agy) so AgyCliService can spawn it
RUN apt-get update && apt-get install -y curl ca-certificates --no-install-recommends \
    && curl -fsSL https://antigravity.google/cli/install.sh | bash \
    && apt-get clean && rm -rf /var/lib/apt/lists/*

# Copy the published output
COPY --from=build-env /app/out .

# Handle .NET 8 port configuration; also expose agy install location to PATH
ENV ASPNETCORE_HTTP_PORTS=8080
ENV PATH="/root/.local/bin:${PATH}"
EXPOSE 8080

VOLUME [ "./mnt" ]
ENTRYPOINT ["dotnet", "simplebibleapp.dll"]