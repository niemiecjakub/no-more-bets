FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build
WORKDIR /app

COPY ["NoMoreBets.csproj", "."]
RUN dotnet restore "NoMoreBets.csproj"

COPY . .
RUN dotnet build "NoMoreBets.csproj" -c Release -o /app/build

FROM build AS publish
RUN dotnet publish "NoMoreBets.csproj" -c Release -o /app/publish

FROM mcr.microsoft.com/dotnet/aspnet:9.0
WORKDIR /app
EXPOSE 8080
EXPOSE 8081
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "NoMoreBets.dll"]
