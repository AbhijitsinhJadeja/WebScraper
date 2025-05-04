using Serilog;
using Services;

Console.WriteLine($"Current Directory: {Directory.GetCurrentDirectory()}");

if (!Directory.Exists("logs"))
{
    Directory.CreateDirectory("logs");
}

Log.Logger = new LoggerConfiguration()                                  
           .MinimumLevel.Information()
           .WriteTo.Console()
           .WriteTo.File("logs/log-.txt", rollingInterval: RollingInterval.Day)                 //  --LogFilePath: WebScraper\bin\Debug\net8.0\logs
           .CreateLogger();

string connectionString = "Host=localhost;Username=postgres;Password=admin;Database=WebScraper";

try
{
    Log.Information("Application is starting...");
    
    var repository = new MediaRepository(connectionString, Log.Logger);
    var scraperService = new ScraperService(repository);

    scraperService.ScrapeAndSaveData("https://www.hotstar.com/in");
}
catch (Exception ex)
{
    Log.Fatal(ex, "An unhandled exception occurred.");
}
finally
{
    Log.CloseAndFlush();
}