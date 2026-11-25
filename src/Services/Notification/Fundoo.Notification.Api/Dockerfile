FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

COPY . .

RUN dotnet restore "src/Services/Notification/Fundoo.Notification.Api/Fundoo.Notification.Api.csproj"

RUN dotnet publish "src/Services/Notification/Fundoo.Notification.Api/Fundoo.Notification.Api.csproj" -c Release -o /app/publish --no-restore

FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS final
WORKDIR /app

ENV ASPNETCORE_URLS=http://+:8080
EXPOSE 8080

COPY --from=build /app/publish .

ENTRYPOINT ["dotnet", "Fundoo.Notification.Api.dll"]
