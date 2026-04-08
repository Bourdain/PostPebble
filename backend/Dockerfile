FROM mcr.microsoft.com/dotnet/sdk:10.0-preview AS build
WORKDIR /src

COPY backend/src/Api/Api.csproj backend/src/Api/
RUN dotnet restore backend/src/Api/Api.csproj

COPY backend/src/Api/ backend/src/Api/
RUN dotnet publish backend/src/Api/Api.csproj -c Release -o /app/publish /p:UseAppHost=false

FROM mcr.microsoft.com/dotnet/aspnet:10.0-preview
WORKDIR /app
COPY --from=build /app/publish .

EXPOSE 8080
ENTRYPOINT ["dotnet", "Api.dll"]
