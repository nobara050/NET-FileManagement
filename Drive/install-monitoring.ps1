
# Implement Prometheus + Loki + Tempo vào Drive.Api
# Chạy từ thư mục Drive/

Write-Host "=== Cài đặt packages Prometheus ===" -ForegroundColor Cyan
dotnet add Drive.Api/Drive.Api.csproj package prometheus-net.AspNetCore

Write-Host "=== Cài đặt packages Serilog + Loki ===" -ForegroundColor Cyan
dotnet add Drive.Api/Drive.Api.csproj package Serilog.AspNetCore
dotnet add Drive.Api/Drive.Api.csproj package Serilog.Sinks.Grafana.Loki
dotnet add Drive.Api/Drive.Api.csproj package Serilog.Enrichers.Span

Write-Host "=== Cài đặt packages OpenTelemetry + Tempo ===" -ForegroundColor Cyan
dotnet add Drive.Api/Drive.Api.csproj package OpenTelemetry.Extensions.Hosting
dotnet add Drive.Api/Drive.Api.csproj package OpenTelemetry.Instrumentation.AspNetCore
dotnet add Drive.Api/Drive.Api.csproj package OpenTelemetry.Instrumentation.Http
dotnet add Drive.Api/Drive.Api.csproj package OpenTelemetry.Exporter.OpenTelemetryProtocol
dotnet add Drive.Infrastructure/Drive.Infrastructure.csproj package Npgsql.OpenTelemetry

Write-Host "=== Tất cả packages đã cài đặt thành công ===" -ForegroundColor Green
