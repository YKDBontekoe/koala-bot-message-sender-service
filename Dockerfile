FROM mcr.microsoft.com/dotnet/sdk:6.0-bullseye-slim AS build
WORKDIR /src
COPY ["MessagePublisherService/MessagePublisherService.csproj", "MessagePublisherService/"]
COPY ["Infrastructure.Messaging/Infrastructure.Messaging.csproj", "Infrastructure.Messaging/"]
RUN dotnet restore "KoalaMessageSenderService/KoalaMessageSenderService.csproj"

COPY . .
WORKDIR "/src/KoalaMessageSenderService"
RUN dotnet build "KoalaMessageSenderService.csproj" -c Release -o /app/build

FROM build AS publish
RUN dotnet publish "KoalaMessageSenderService.csproj" -c Release -o /app/publish

FROM mcr.microsoft.com/dotnet/aspnet:6.0-bullseye-slim AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "Koala.MessageSenderService.dll"]
