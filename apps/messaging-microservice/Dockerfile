# Build stage
FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build
WORKDIR /src

COPY src/MessagingService/MessagingService.csproj src/MessagingService/
RUN dotnet restore src/MessagingService/MessagingService.csproj

COPY src/MessagingService/ src/MessagingService/
RUN dotnet publish src/MessagingService/MessagingService.csproj -c Release -o /app/publish

# Runtime stage
FROM mcr.microsoft.com/dotnet/aspnet:9.0 AS runtime
WORKDIR /app
COPY --from=build /app/publish .
EXPOSE 8080
ENTRYPOINT ["dotnet", "MessagingService.dll"]
