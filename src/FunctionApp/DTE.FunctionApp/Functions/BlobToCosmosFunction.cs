using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Logging;
using DTE.FunctionApp.Services;
using System.IO;
using System.Text.Json;
using System.Linq;
using System.Security.Cryptography;
using System.Text;

namespace DTE.FunctionApp.Functions
{
    public class BlobToCosmosFunction(ILogger<BlobToCosmosFunction> logger, IExcelToJsonService excelToJsonService)
    {
        [Function(nameof(BlobToCosmosFunction))]
        [CosmosDBOutput("unite_surveys", "collected_data", Connection = "CosmosDBConnectionString")]
        public async Task<object[]> Run(
            [BlobTrigger("surveys/{name}", Connection = "StorageAccountConnectionString")] byte[] blobContent,
            string name)
        {
            logger.LogInformation($"C# Blob trigger function processing blob: {name}");

            try
            {
                // Convert byte array to stream for processing
                using var blobStream = new MemoryStream(blobContent);

                // Process XLSX file and convert to JSON using the injected service
                var jsonData = await excelToJsonService.ConvertXlsxToJsonAsync(blobStream, name);

                // Deserialize JSON to array of dictionaries for processing
                var surveyResponses = JsonSerializer.Deserialize<Dictionary<string, object>[]>(jsonData);

                // Transform each response to include required Cosmos DB fields
                var cosmosDocuments = surveyResponses?.Select((response, index) => 
                {
                    var document = new Dictionary<string, object>(response);
                    
                    // Generate deterministic ID using RespondentID and CollectorID
                    var respondantId = response.GetValueOrDefault("RespondentID")?.ToString() ?? throw new Exception("no field 'RespondentID' in JSON");
                    var collectorId = response.GetValueOrDefault("CollectorID")?.ToString() ?? throw new Exception("no field 'CollectorID' in JSON");
                    var deterministicId = GenerateDeterministicId(respondantId, collectorId);
                    
                    document["id"] = deterministicId;
                    
                    // Add metadata
                    document["sourceFile"] = name;
                    document["processedAt"] = DateTime.UtcNow;
                    document["recordIndex"] = index;
                    
                    return document;
                }).ToArray() ?? new Dictionary<string, object>[0];

                logger.LogInformation($"Successfully processed blob {name} and prepared {cosmosDocuments.Length} records for Cosmos DB");
                
                // Return the array of properly formatted documents for Cosmos DB
                return cosmosDocuments.Cast<object>().ToArray();

            }
            catch (Exception ex)
            {
                logger.LogError(ex, $"Error processing blob {name}: {ex.Message}");
                throw;
            }
        }

        private static string GenerateDeterministicId(string respondantId, string collectorId)
        {
            // Combine the two IDs to create a unique string
            var combinedString = $"{respondantId}_{collectorId}";
            
            // Use SHA256 to create a hash of the combined string
            using var sha256 = SHA256.Create();
            var hashBytes = sha256.ComputeHash(Encoding.UTF8.GetBytes(combinedString));
            
            // Convert the first 16 bytes to a GUID format
            var guidBytes = new byte[16];
            Array.Copy(hashBytes, guidBytes, 16);
            
            return new Guid(guidBytes).ToString();
        }
    }
}
