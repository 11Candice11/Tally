# ── Build stage ───────────────────────────────────────────────────────────────
FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build
WORKDIR /src

# Inject host system root CAs to fix SSL PartialChain errors (corporate/VPN certs)
COPY system-certs.pem /usr/local/share/ca-certificates/system-certs.crt
RUN update-ca-certificates

COPY API/InvoiceTrackerAPI2/InvoiceTrackerAPI2.csproj API/InvoiceTrackerAPI2/
RUN --mount=type=cache,target=/root/.nuget/packages \
    dotnet restore API/InvoiceTrackerAPI2/InvoiceTrackerAPI2.csproj

COPY API/InvoiceTrackerAPI2/ API/InvoiceTrackerAPI2/
RUN --mount=type=cache,target=/root/.nuget/packages \
    dotnet publish API/InvoiceTrackerAPI2/InvoiceTrackerAPI2.csproj -c Release -o /app/publish --no-restore

# ── Runtime stage ─────────────────────────────────────────────────────────────
FROM mcr.microsoft.com/dotnet/aspnet:9.0 AS runtime
WORKDIR /app

RUN apt-get update && apt-get install -y --no-install-recommends curl && rm -rf /var/lib/apt/lists/*

RUN adduser --disabled-password --gecos "" appuser

# Persist the SQLite database outside the container
VOLUME /app/data

COPY --from=build /app/publish .
RUN chown -R appuser /app
USER appuser

ENV ASPNETCORE_URLS=http://+:8080

EXPOSE 8080

HEALTHCHECK --interval=30s --timeout=5s --start-period=15s --retries=3 \
  CMD curl -f http://localhost:8080/healthz/live || exit 1

ENTRYPOINT ["dotnet", "InvoiceTrackerAPI2.dll"]
