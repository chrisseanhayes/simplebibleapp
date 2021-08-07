FROM microsoft/dotnet:2.1-sdk AS build-env
WORKDIR /app

COPY . ./

RUN dotnet publish ./simplebibleapp/simplebibleapp.csproj -c Release -o /app/out

FROM microsoft/dotnet:2.1-aspnetcore-runtime
WORKDIR /app/out
COPY --from=build-env /app/out .
ENV ASPNETCORE_URLS http://+:5000
EXPOSE 5000
VOLUME [ "./mnt" ]
ENTRYPOINT ["dotnet", "simplebibleapp.dll"]
