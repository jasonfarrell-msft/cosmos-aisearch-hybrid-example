using DTE.FunctionApp.Extensions;
using DTE.FunctionApp.Models;
using DTE.FunctionApp.Services;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;

namespace DTE.FunctionApp.Functions
{
    public class CosmosToSearchIndexFunction(RespondentDataService respondentDataService, QuestionAnswerDataService questionAnswerDataService,
        ISearchIndexService searchIndexService, ILogger<CosmosToSearchIndexFunction> logger)
    {
        [Function("CosmosToSearchIndex")]
        public async Task Run(
            [CosmosDBTrigger("unite_surveys", "collected_data", Connection = "CosmosDBConnectionString",
                LeaseContainerName = "collected_data_leases",
                CreateLeaseContainerIfNotExists = true)] IReadOnlyList<JsonElement> documents)
        {
            foreach (var document in documents)
            {
                var documentId = document.GetPropertyValue<string>("id");
                logger.LogInformation($"Processing document with id: {documentId}");

                logger.LogInformation("Extracting Respondent Data...");
                var respondentData = await respondentDataService.GetRespondentDataAsync(document);

                logger.LogInformation("Extracting Question and Answer Data...");
                var questionAnswers = await questionAnswerDataService.GetQuestionAnswersAsync(document);

                logger.LogInformation("Creating Search Index Entries...");
                var indexEntries = questionAnswers
                    .Select(qa => new SurveyData
                    {
                        RespondentId = respondentData.RespondentId,
                        SurveyId = respondentData.CollectorId,
                        EmailAddress = respondentData.EmailAddress,
                        Name = $"{respondentData.FirstName} {respondentData.LastName}",
                        IPAddress = respondentData.IPAddress,
                        PhoneNumber = respondentData.PhoneNumber,
                        SurveyName = respondentData.SurveyName,
                        SurveyStart = respondentData.SurveyStart,
                        SurveyEnd = respondentData.SurveyEnd,
                        Question = qa.Question,
                        Answer = qa.Answer,
                    })
                    .Where(x => string.IsNullOrEmpty(x.Answer) == false)
                    .ToList();

                logger.LogInformation($"Adding {indexEntries.Count} to Search Index");
                await searchIndexService.AddEntriesToIndex(indexEntries);
            }

            logger.LogInformation($"Finished");
        }
    }
}
