using SmartServe.Infrastructure.Identity;
using SmartServe.Infrastructure.Persistence;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using SmartServe.Application.Storage;
using SmartServe.Infrastructure.Storage;

namespace SmartServe.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        var connectionString = configuration.GetConnectionString("SmartServeDatabase")
            ?? throw new InvalidOperationException(
                "Connection string 'SmartServeDatabase' was not configured.");

        services.AddDbContext<ApplicationDbContext>(options =>
            options.UseSqlServer(connectionString));
        services.Configure<DatabaseMigrationOptions>(configuration.GetSection(DatabaseMigrationOptions.SectionName));
        services.AddScoped<SchemaBaselineValidator>();
        services.AddOptions<FileStorageOptions>()
            .Bind(configuration.GetSection(FileStorageOptions.SectionName))
            .Validate(value => !string.IsNullOrWhiteSpace(value.ConnectionString) ||
                               (value.UseManagedIdentity && value.ServiceUri is not null),
                "File storage requires a connection string or a service URI with managed identity.")
            .ValidateOnStart();
        services.AddSingleton<IFileStorage, AzureBlobFileStorage>();

        services
            .AddIdentityCore<ApplicationUser>(options =>
            {
                options.User.RequireUniqueEmail = true;
                options.Password.RequiredLength = 10;
                options.Password.RequireDigit = true;
                options.Password.RequireLowercase = true;
                options.Password.RequireUppercase = true;
                options.Password.RequireNonAlphanumeric = true;
            })
            .AddRoles<IdentityRole<Guid>>()
            .AddEntityFrameworkStores<ApplicationDbContext>();

        return services;
    }
}
