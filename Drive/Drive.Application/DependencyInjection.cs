using FluentValidation;
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

        return services;
    }
}