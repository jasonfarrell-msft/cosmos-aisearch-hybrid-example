using Azure;
using Azure.Search.Documents;
using Azure.Search.Documents.Models;
using DTE.FunctionApp.Models;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using System.Text.Json;

namespace DTE.FunctionApp.Services
{
    public class AzureSearchIndexService : ISearchIndexService
    {
        private readonly SearchClient _searchClient;
        private readonly ILogger<AzureSearchIndexService> _logger;

        public AzureSearchIndexService(IConfiguration configuration, ILogger<AzureSearchIndexService> logger)
        {
            _logger = logger;

            // Get Azure Search configuration from settings
            string searchEndpoint = configuration["SearchEndpoint"];
            string searchAdminKey = configuration["SearchAdminKey"];
            string searchIndexName = configuration["SearchIndexName"];

            if (string.IsNullOrEmpty(searchEndpoint) || string.IsNullOrEmpty(searchAdminKey) || string.IsNullOrEmpty(searchIndexName))
            {
                _logger.LogError("Azure Search configuration is missing. Make sure SearchEndpoint, SearchAdminKey, and SearchIndexName are set in configuration.");
                throw new InvalidOperationException("Azure Search configuration is missing");
            }

            // Create a SearchClient using the endpoint, credential, and index name
            var searchCredential = new AzureKeyCredential(searchAdminKey);
            _searchClient = new SearchClient(new Uri(searchEndpoint), searchIndexName, searchCredential);

            _logger.LogInformation($"Connected to Azure Search index '{searchIndexName}' at {searchEndpoint}");
        }

        public async Task AddEntriesToIndex(List<SurveyData> entries)
        {
            if (entries == null || entries.Count == 0)
            {
                _logger.LogInformation("No entries to add to the search index");
                return;
            }

            _logger.LogInformation($"Adding {entries.Count} entries to search index");


            // Azure Search operations work better in batches
            // Recommended batch size is usually around 1000 documents
            const int batchSize = 50;

            for (int i = 0; i < entries.Count; i += batchSize)
            {
                var batch = entries.Skip(i).Take(batchSize).ToList();

                // Create a batch of documents to index
                var actions = batch.Select(entry => IndexDocumentsAction.Upload(entry)).ToList();

                // Explicitly specify the type parameter for IndexDocumentsBatch.Create
                var indexBatch = IndexDocumentsBatch.Create<SurveyData>([.. actions]);

                // Upload documents to the index
                IndexDocumentsResult result = await _searchClient.IndexDocumentsAsync(indexBatch);

                _logger.LogInformation($"Batch {i / batchSize + 1}: {result.Results.Count} documents indexed with {result.Results.Count(r => r.Succeeded)} succeeded and {result.Results.Count(r => !r.Succeeded)} failed");

                // Log any failures in detail
                foreach (var item in result.Results.Where(r => !r.Succeeded))
                {
                    _logger.LogError($"Failed to index document key {item.Key}: {item.ErrorMessage}");
                }
            }

            _logger.LogInformation("Completed adding entries to the search index");

        }
    }

    public interface ISearchIndexService
    {
        Task AddEntriesToIndex(List<SurveyData> entries);
    }
}