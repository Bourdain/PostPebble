FROM mcr.microsoft.com/dotnet/sdk:10.0-preview AS build
WORKDIR /src

COPY backend/src/Api/Api.csproj backend/src/Api/
COPY backend/src/DbMigrator/DbMigrator.csproj backend/src/DbMigrator/
RUN dotnet restore backend/src/DbMigrator/DbMigrator.csproj

COPY backend/src/Api/ backend/src/Api/
COPY backend/src/DbMigrator/ backend/src/DbMigrator/
RUN dotnet publish backend/src/DbMigrator/DbMigrator.csproj -c Release -o /app/publish /p:UseAppHost=false

FROM mcr.microsoft.com/dotnet/aspnet:10.0-preview
WORKDIR /app
COPY --from=build /app/publish .

ENTRYPOINT ["dotnet", "DbMigrator.dll"]
