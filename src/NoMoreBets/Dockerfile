FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build
WORKDIR /app

COPY ["NoMoreBets.csproj", "."]
RUN dotnet restore "NoMoreBets.csproj"

COPY . .
RUN dotnet build "NoMoreBets.csproj" -c Release -o /app/build

FROM build AS publish
RUN dotnet publish "NoMoreBets.csproj" -c Release -o /app/publish

FROM mcr.microsoft.com/playwright/dotnet:v1.49.0-noble AS final
WORKDIR /app

COPY --from=build /usr/share/dotnet/shared/Microsoft.NETCore.App /usr/share/dotnet/shared/Microsoft.NETCore.App
COPY --from=build /usr/share/dotnet/shared/Microsoft.AspNetCore.App /usr/share/dotnet/shared/Microsoft.AspNetCore.App

ENV ASPNETCORE_URLS=http://+:8080
EXPOSE 8080
EXPOSE 8081

COPY --from=publish /app/publish .

RUN pwsh /app/playwright.ps1 install chromium

ENTRYPOINT ["dotnet", "NoMoreBets.dll"]