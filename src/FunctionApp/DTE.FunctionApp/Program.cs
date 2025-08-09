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

        builder.Services.AddScoped<IExcelToJsonService, ExcelToJsonService>();
        builder.Services.AddScoped<RespondentDataService>();
        builder.Services.AddScoped<QuestionAnswerDataService>();
        builder.Services.AddScoped<ISearchIndexService, AzureSearchIndexService>();
        builder.Services.AddScoped<IChatService, AzureOpenAiChatService>();

        // Application Insights isn't enabled by default. See https://aka.ms/AAt8mw4.
        // builder.Services
        //     .AddApplicationInsightsTelemetryWorkerService()
        //     .ConfigureFunctionsApplicationInsights();

        builder.Build().Run();
    }
}
