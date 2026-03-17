# ── Build stage ──────────────────────────────────────────────
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

# Copy project file first for layer-cached restore
COPY ChatBot-BE/ChatBot-BE.csproj ChatBot-BE/
RUN dotnet restore ChatBot-BE/ChatBot-BE.csproj

# Copy everything else and publish
COPY . .
WORKDIR /src/ChatBot-BE
RUN dotnet publish -c Release -o /app/publish --no-restore

# ── Runtime stage ────────────────────────────────────────────
FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS final
WORKDIR /app

EXPOSE 8080
ENV ASPNETCORE_URLS=http://+:8080

COPY --from=build /app/publish .

ENTRYPOINT ["dotnet", "ChatBot-BE.dll"]
