using Amazon.S3;
using Drive.Application.Common.Interfaces;
using Drive.Infrastructure.Authentication;
using Drive.Infrastructure.Authorization;
using Drive.Infrastructure.Identity;
using Drive.Infrastructure.Persistence;
using Drive.Infrastructure.Persistence.Queries;
using Drive.Infrastructure.Persistence.Repositories;
using Drive.Infrastructure.Seeding;
using Drive.Infrastructure.Storage.S3;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Drive.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        var connectionString =
            configuration.GetConnectionString("DefaultConnection");

        // Add DbContext with PostgreSQL provider
        // Npgsql OpenTelemetry tracing được đăng ký qua AddNpgsql() trong OTel builder tại Program.cs
        services.AddDbContext<DriveDbContext>(options =>
            options.UseNpgsql(connectionString));

        // Add Repository
        services.AddScoped(typeof(IRepository<>), typeof(Repository<>));
        services.AddScoped<IDriveItemRepository, DriveItemRepository>();
        services.AddScoped<IDriveItemRoleAssignmentRepository, DriveItemRoleAssignmentRepository>();

        // Add Drive item access query
        services.AddScoped<IDriveItemAccessQuery, DriveItemAccessQuery>();
        services.AddScoped<ISharedDriveItemQuery, SharedDriveItemQuery>();

        // Add Data Protection
        services.AddDataProtection();

        // Add Identity services
        services.AddIdentityCore<ApplicationUser>(options =>
        {
            options.User.RequireUniqueEmail = true;

            options.Password.RequireDigit = true;
            options.Password.RequireLowercase = true;
            options.Password.RequireUppercase = true;
            options.Password.RequireNonAlphanumeric = true;
            options.Password.RequiredLength = 8;
        })
        .AddRoles<IdentityRole<Guid>>()
        .AddEntityFrameworkStores<DriveDbContext>()
        .AddSignInManager()
        .AddDefaultTokenProviders();

        // Configure JWT options
        services.Configure<JwtOptions>(
            configuration.GetSection(JwtOptions.SectionName));

        services.AddScoped<IJwtTokenService, JwtTokenService>();

        services.AddScoped<IRefreshTokenService, RefreshTokenService>();

        services.AddHttpContextAccessor();

        services.AddScoped<ICurrentUserService, CurrentUserService>();

        // Add Authorization services
        services.AddScoped<IPermissionService, PermissionService>();

        // Add Permission Materializer
        services.AddScoped<IPermissionMaterializer, PermissionMaterializer>();

        // Add Identity Service
        services.AddScoped<IIdentityService, IdentityService>();
        services.AddScoped<IUserManagementService, UserManagementService>();
        services.AddScoped<IRoleService, RoleService>();

        // Add Seeder
        services.AddScoped<Seeder>();

        // Configure S3 options
        services.Configure<S3Options>(
            configuration.GetSection(S3Options.SectionName));

        var s3Options = configuration
        .GetSection(S3Options.SectionName)
        .Get<S3Options>();

        if (s3Options is null)
        {
            throw new InvalidOperationException(
                "S3 configuration is missing.");
        }

        services.AddSingleton<IAmazonS3>(_ =>
        {
            var config = new AmazonS3Config
            {
                ServiceURL = s3Options.ServiceUrl,
                AuthenticationRegion = s3Options.Region,
                ForcePathStyle = true
            };

            return new AmazonS3Client(
                "test",
                "test",
                config);
        });

        services.AddScoped<IFileStorage, S3FileStorage>();

        return services;
    }
}