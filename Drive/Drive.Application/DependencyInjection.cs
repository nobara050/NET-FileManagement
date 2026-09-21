using Drive.Application.Common.Behaviors;
using Drive.Application.Features.Auth;
using Drive.Application.Features.DriveItems;
using FluentValidation;
using MediatR;
using Microsoft.Extensions.DependencyInjection;

namespace Drive.Application;

public static class DependencyInjection
{
    public static IServiceCollection AddApplication(
        this IServiceCollection services)
    {
        services.AddAutoMapper(
            _ => { },
            typeof(DependencyInjection).Assembly);

        services.AddValidatorsFromAssembly(
            typeof(DependencyInjection).Assembly);

        services.AddMediatR(cfg =>
        {
            cfg.RegisterServicesFromAssembly(typeof(DependencyInjection).Assembly);
            // TracingBehavior đứng đầu pipeline để span bao trùm cả validation + handler
            cfg.AddOpenBehavior(typeof(TracingBehavior<,>));
            cfg.AddOpenBehavior(typeof(ValidationBehavior<,>));
        });

        return services;
    }
}