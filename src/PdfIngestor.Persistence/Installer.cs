using System.Diagnostics;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace PdfIngestor.Persistence;

public static class Installer
{
    public static void AddPersistence(this IServiceCollection services, IConfiguration configuration)
    {
        const string connectionStringName = "Default";
        var connectionString = configuration.GetConnectionString(connectionStringName);
        
        if (string.IsNullOrEmpty(connectionString))
        {
            throw new InvalidOperationException($"{connectionString} must be filled in.");
        }
        
        services.AddDbContext<AppDbContext>(options =>
        {
            options.UseNpgsql(connectionString);
            
#if DEBUG
            options.EnableSensitiveDataLogging();
            options.EnableDetailedErrors();
            options.LogTo(x => Debug.WriteLine(x));
#endif
        });
    }
    
    public static void Migrate(this IServiceProvider serviceProvider)
    {
        using var scope = serviceProvider.CreateScope();
        var logger = scope.ServiceProvider.GetRequiredService<ILogger<AppDbContext>>();
        logger.LogInformation("Using migrations.");

        var dbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();

        dbContext.Database.Migrate();
        logger.LogInformation("Using migrations - OK.");
    }           
}