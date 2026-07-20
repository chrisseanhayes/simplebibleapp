# 1. Use the new .NET 8 SDK image for building
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build-env
WORKDIR /app

# Copy everything and publish
COPY . ./
RUN dotnet publish ./simplebibleapp/simplebibleapp.csproj -c Release -o /app/out

# 2. Use the modern ASP.NET Core runtime image
FROM mcr.microsoft.com/dotnet/aspnet:8.0
WORKDIR /app
# Copy the published output directly into the working directory
COPY --from=build-env /app/out .

# 3. Handle the modern .NET 8 port configurations (Defaults to 8080)
ENV ASPNETCORE_HTTP_PORTS=8080
EXPOSE 8080

VOLUME [ "./mnt" ]
ENTRYPOINT ["dotnet", "simplebibleapp.dll"]