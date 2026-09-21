using Drive.Api.Authorization;
using Drive.Api.Exceptions;
using Drive.Api.Mapping;
using Drive.Application;
using Drive.Application.Common.Authorization;
using Drive.Infrastructure;
using Drive.Infrastructure.Authentication;
using Drive.Infrastructure.Seeding;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Mvc.Controllers;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi;
using OpenTelemetry.Exporter;
using OpenTelemetry.Resources;
using OpenTelemetry.Trace;
using Prometheus;
using Serilog;
using Serilog.Enrichers.Span;
using Serilog.Sinks.Grafana.Loki;
using System.Text;
using Npgsql;

// ============================================================
// Configure Serilog: Console + Loki (with TraceId enrichment)
// ============================================================
var outputTemplate = "[{Timestamp:HH:mm:ss} {Level:u3}] [TraceId: {TraceId}] {Message:lj}{NewLine}{Exception}";

Log.Logger = new LoggerConfiguration()
    .Enrich.FromLogContext()
    .Enrich.WithSpan()  // Gắn TraceId / SpanId tự động vào từng log
    .WriteTo.Console(outputTemplate: outputTemplate)
    .WriteTo.GrafanaLoki(
        "http://localhost:3100",
        labels:
        [
            new LokiLabel { Key = "app", Value = "drive-api" },
            new LokiLabel { Key = "environment", Value = "development" }
        ],
        textFormatter: new LokiJsonTextFormatter())
    .CreateLogger();

var builder = WebApplication.CreateBuilder(args);

// Sử dụng Serilog thay thế logging mặc định của ASP.NET Core
builder.Host.UseSerilog();

// Add services to the container.

builder.Services.AddControllers();

builder.Services.AddExceptionHandler<ValidationExceptionHandler>();
builder.Services.AddExceptionHandler<ApplicationExceptionHandler>();
builder.Services.AddProblemDetails();

builder.Services.AddAutoMapper(
    _ => { },
    typeof(ApiMappingProfile).Assembly);

builder.Services.AddEndpointsApiExplorer();

builder.Services.AddSwaggerGen(options =>
{
    options.TagActionsBy(api =>
    {
        if (api.ActionDescriptor is not ControllerActionDescriptor controller)
        {
            return ["Other"];
        }

        var namespaceParts =
            (controller.ControllerTypeInfo.Namespace ?? string.Empty)
            .Split('.', StringSplitOptions.RemoveEmptyEntries);

        var featuresIndex =
            Array.FindIndex(
                namespaceParts,
                part => part == "Features");

        if (featuresIndex >= 0 &&
            featuresIndex + 1 < namespaceParts.Length)
        {
            return [namespaceParts[featuresIndex + 1]];
        }

        return ["Other"];
    });

    options.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
    {
        Name = "Authorization",
        Type = SecuritySchemeType.Http,
        Scheme = "bearer",
        BearerFormat = "JWT",
        In = ParameterLocation.Header,
        Description = "Enter your JWT access token."
    });

    options.AddSecurityRequirement(document =>
        new OpenApiSecurityRequirement
        {
            [new OpenApiSecuritySchemeReference("Bearer", document)] = []
        });
});

builder.Services.AddApplication();

builder.Services.AddInfrastructure(
    builder.Configuration);

// ============================================================
// Configure OpenTelemetry: Traces -> Tempo (OTLP gRPC :4317)
// ============================================================
builder.Services.AddOpenTelemetry()
    .WithTracing(tracing => tracing
        .SetResourceBuilder(
            ResourceBuilder.CreateDefault()
                .AddService("drive-api"))
        .AddAspNetCoreInstrumentation(options =>
        {
            // Bỏ qua trace cho /metrics endpoint
            options.Filter = ctx => !ctx.Request.Path.StartsWithSegments("/metrics");
            options.RecordException = true;
        })
        .AddHttpClientInstrumentation(options =>
        {
            // Bỏ qua trace cho các request gửi log tới Loki và gửi trace tới Tempo
            options.FilterHttpRequestMessage = req =>
            {
                var uri = req.RequestUri?.ToString() ?? string.Empty;
                return !uri.Contains(":3100") && !uri.Contains(":4317") && !uri.Contains(":4318");
            };
        })
        .AddEntityFrameworkCoreInstrumentation()
        .AddNpgsql()
        .AddSource("Drive.Application")      // Span cho Application layer (MediatR handlers)
        .AddSource("Drive.Infrastructure")   // Span cho Infrastructure layer (Repositories, Services, Queries)
        .AddOtlpExporter(opt =>
        {
            opt.Endpoint = new Uri("http://localhost:4317");
            opt.Protocol = OtlpExportProtocol.Grpc;
        }));

var jwtOptions = builder.Configuration
    .GetSection(JwtOptions.SectionName)
    .Get<JwtOptions>();

if (jwtOptions is null)
{
    throw new InvalidOperationException(
        "JWT configuration is missing.");
}

builder.Services
    .AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.MapInboundClaims = false;

        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidIssuer = jwtOptions.Issuer,

            ValidateAudience = true,
            ValidAudience = jwtOptions.Audience,

            ValidateIssuerSigningKey = true,
            IssuerSigningKey = new SymmetricSecurityKey(
                Encoding.UTF8.GetBytes(jwtOptions.SecretKey)),

            ValidateLifetime = true,
            ClockSkew = TimeSpan.Zero
        };

        options.Events = new JwtBearerEvents
        {
            OnAuthenticationFailed = context =>
            {
                Console.WriteLine(
                    $"JWT authentication failed: {context.Exception.Message}");

                return Task.CompletedTask;
            },

            OnChallenge = context =>
            {
                Console.WriteLine(
                    $"JWT challenge error: {context.Error}, {context.ErrorDescription}");

                return Task.CompletedTask;
            }
        };
    });

builder.Services.AddAuthorization(options =>
{
    options.AddPolicy("Admin", policy => policy.Requirements.Add(new AdminRequirement()));

    options.AddPolicy(Permissions.DriveRead, policy =>
        policy.Requirements.Add(new PermissionRequirement(Permissions.DriveRead)));
    options.AddPolicy(Permissions.DriveDownload, policy =>
        policy.Requirements.Add(new PermissionRequirement(Permissions.DriveDownload)));
    options.AddPolicy(Permissions.DriveDelete, policy =>
        policy.Requirements.Add(new PermissionRequirement(Permissions.DriveDelete)));
});

builder.Services.AddScoped<IAuthorizationHandler, PermissionAuthorizationHandler>();
builder.Services.AddScoped<IAuthorizationHandler, AdminAuthorizationHandler>();

var app = builder.Build();

// Seed the database with initial data
using (var scope = app.Services.CreateScope())
{
    var seeder = scope.ServiceProvider
        .GetRequiredService<Seeder>();

    await seeder.SeedAsync();
}

// Configure the HTTP request pipeline.

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();

app.UseExceptionHandler();

app.UseAuthentication();
app.UseAuthorization();

// ============================================================
// Prometheus: expose /metrics endpoint để Prometheus scrape
// ============================================================
app.UseHttpMetrics();  // Tự động thu thập http_requests_received_total, http_request_duration_seconds
app.MapMetrics();      // Expose /metrics

app.MapControllers();

app.Run();