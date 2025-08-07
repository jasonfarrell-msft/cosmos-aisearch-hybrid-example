using System;
using System.Collections.Generic;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Logging;

namespace DTE.FunctionApp.Functions
{
    public class CosmosToSearchIndexFunction(ILogger<CosmosToSearchIndexFunction> logger)
    {
       
        [Function("CosmosToSearchIndex")]
        public async Task Run(
            [CosmosDBTrigger("unite_surveys", "collected_data", Connection = "CosmosDBConnectionString",
                LeaseContainerName = "collected_data_leases",
                CreateLeaseContainerIfNotExists = true)] IReadOnlyList<MyDocument> input)
        {
            if (input != null && input.Count > 0)
            {
                _logger.LogInformation("Documents modified: " + input.Count);
                _logger.LogInformation("First document Id: " + input[0].id);
            }
        }
    }
}
