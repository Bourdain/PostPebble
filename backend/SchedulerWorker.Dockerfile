FROM mcr.microsoft.com/dotnet/sdk:10.0-preview AS build
WORKDIR /src

COPY backend/src/Api/Api.csproj backend/src/Api/
COPY backend/src/SchedulerWorker/SchedulerWorker.csproj backend/src/SchedulerWorker/
RUN dotnet restore backend/src/SchedulerWorker/SchedulerWorker.csproj

COPY backend/src/Api/ backend/src/Api/
COPY backend/src/SchedulerWorker/ backend/src/SchedulerWorker/
RUN dotnet publish backend/src/SchedulerWorker/SchedulerWorker.csproj -c Release -o /app/publish /p:UseAppHost=false

FROM mcr.microsoft.com/dotnet/aspnet:10.0-preview
WORKDIR /app
COPY --from=build /app/publish .

ENTRYPOINT ["dotnet", "SchedulerWorker.dll"]
