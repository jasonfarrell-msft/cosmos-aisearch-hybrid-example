using Microsoft.Azure.Functions.Worker.Builder;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.DependencyInjection;
using DTE.FunctionApp.Services;
// Add the correct using directive for IExcelToJsonService if it's in a different namespace
// using YourNamespace.Services;

namespace DTE.FunctionApp;

public class Program
{
    public static void Main(string[] args)
    {
        var builder = FunctionsApplication.CreateBuilder(args);

        builder.ConfigureFunctionsWebApplication();
        // Ensure IExcelToJsonService is defined in your project and the correct using directive is present above
        builder.Services.AddScoped<IExcelToJsonService, ExcelToJsonService>();
    

        // Application Insights isn't enabled by default. See https://aka.ms/AAt8mw4.
        // builder.Services
        //     .AddApplicationInsightsTelemetryWorkerService()
        //     .ConfigureFunctionsApplicationInsights();

        builder.Build().Run();
    }
}
