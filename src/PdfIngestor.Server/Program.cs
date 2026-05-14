using PdfIngestor.App;
using PdfIngestor.App.Configs;
using PdfIngestor.Persistence;
using PdfIngestor.Server;
using NLog;
using NLog.Web;

var logger = LogManager.Setup().LoadConfigurationFromAppSettings().GetCurrentClassLogger();
logger.Debug("Starting...");
try
{
    var builder = WebApplication.CreateBuilder(args);
    builder.Logging.ClearProviders();
    builder.Logging.AddNLogWeb();
    // Add services to the container.

    builder.Services.AddControllers(x => x.Filters.Add<ExceptionFilter>());
    // Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
    builder.Services.AddEndpointsApiExplorer();
    builder.Services.AddSwaggerGen();
    builder.Services.AddPersistence(builder.Configuration);
    builder.Services.AddApp();

    var rabbitMqConfig = builder.Configuration
        .GetRequiredSection("RabbitMq")
        .Get<RabbitMqConfig>()!;

    builder.Services.AddSingleton(rabbitMqConfig);

    var app = builder.Build();

    // Configure the HTTP request pipeline.
    app.UseSwagger();
    app.UseSwaggerUI();

    app.UseAuthorization();

    app.MapControllers();
    app.Services.Migrate();

    app.Run();
}
catch (Exception ex)
{
    logger.Error("Unhandled error {Error}", ex);
    throw;
}
finally
{
    LogManager.Shutdown();
}