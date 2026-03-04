FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build
WORKDIR /src

COPY ["src/NoMoreBets/NoMoreBets.csproj", "NoMoreBets/"]
COPY ["src/NoMoreBets.Application/NoMoreBets.Application.csproj", "NoMoreBets.Application/"]
COPY ["src/NoMoreBets.Domain/NoMoreBets.Domain.csproj", "NoMoreBets.Domain/"]
COPY ["src/NoMoreBets.Infrastructure/NoMoreBets.Infrastructure.csproj", "NoMoreBets.Infrastructure/"]

RUN dotnet restore "NoMoreBets/NoMoreBets.csproj"

COPY src/ .

RUN dotnet build "NoMoreBets/NoMoreBets.csproj" -c Release -o /app/build

FROM build AS publish
RUN dotnet publish "NoMoreBets/NoMoreBets.csproj" -c Release -o /app/publish

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