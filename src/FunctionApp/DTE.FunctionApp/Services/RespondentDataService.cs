using DTE.FunctionApp.Extensions;
using DTE.FunctionApp.Models;
using Microsoft.Extensions.Logging;
using System.Text.Json;

namespace DTE.FunctionApp.Services
{
    public class RespondentDataService(ILogger<RespondentDataService> logger, IChatService chatService)
    {
        public async Task<RespondentData> GetRespondentDataAsync(JsonElement jsonData)
        {
            var respondentData = new RespondentData();
            bool lookingForCompany = false;
            bool lookingForContact = false;

            foreach (var property in jsonData.EnumerateObject())
            {
                string propertyName = property.Name;
                JsonElement propertyValue = property.Value;

                // mode break conditions
                if (lookingForCompany && propertyName.StartsWith("Column") == false)
                {
                    lookingForCompany = false;      // we did not find a valid company
                    logger.LogWarning($"Did not find a valid company for Respondent '{respondentData.RespondentId ?? "Unknown"}' with value '{propertyValue}'");
                }

                if (lookingForContact && propertyName.StartsWith("Column") == false)
                {
                    lookingForContact = false;      // we did not find a valid contact
                    logger.LogWarning($"Did not find a valid contact info for Respondent '{respondentData.RespondentId ?? "Unknown"}' with value '{propertyValue}'");
                }

                // data extraction logic
                bool isDataColumn = IsDataColumn(propertyName);
                if (isDataColumn)
                {
                    /* represents a column that is common to all surverys */
                    UpdateDataColumnProperty(respondentData, propertyName, propertyValue);
                }
                else if (lookingForCompany || StartingCompanyDeclaration(propertyName))
                {
                    lookingForCompany = true;
                    
                    var stringPropertyValue = propertyValue.GetValueAsString();
                    if (string.IsNullOrEmpty(stringPropertyValue) == false && IsValidCompany(stringPropertyValue))
                    {
                        respondentData.Company = stringPropertyValue;
                        lookingForCompany = false; // Stop looking for company after finding a valid one
                    }
                }
                else if (lookingForContact || StartContactInfoDeclaration(propertyName))
                {
                    lookingForContact = true;
                    
                    var stringPropertyValue = propertyValue.GetValueAsString();
                    if (string.IsNullOrEmpty(stringPropertyValue) == false)
                    {
                        await AssignContactInfoValueToObject(respondentData, propertyName, stringPropertyValue);
                    }
                }
            }

            return respondentData;
        }

        bool IsDataColumn(string columnName)
        {
            List<string> dataColumnNames = new()
            {
                "Collector ID",
                "Custom Data 1",
                "Email Address",
                "End Date",
                "First Name",
                "IP Address",
                "Last Name",
                "Respondent ID",
                "Start Date",
                "surveyName"
            };

            return dataColumnNames.Contains(columnName);
        }

        bool IsValidCompany(string? companyName)
        {
            if (string.IsNullOrWhiteSpace(companyName))
            {
                return false;
            }

            return new List<string>()
            {
                "AEP",
                "Ameren",
                "Berkshire Hathaway Energy",
                "Arizona Public Service",
                "CenterPoint Energy",
                "Con Edison",
                "Consumers Energy",
                "Dominion",
                "DTE Energy",
                "Duke Energy",
                "Duquesne Light",
                "Entergy",
                "Eversource",
                "Exelon",
                "NextEra Energy",
                "NiSource",
                "PG&E",
                "PSEG",
                "Sempra",
                "Southern California Edison",
                "Southern Company",
                "Tampa Electric",
                "Tennessee Valley Authority",
                "Xcel Energy"
            }.Contains(companyName.Trim());
        }

        bool StartingCompanyDeclaration(string propertyName)
        {
            return (propertyName.Trim() == "Which Company Do You Represent?");
        }

        bool StartContactInfoDeclaration(string propertyName)
        {
            return propertyName.Trim() == "Please Provide Your Contact Information";
        }

        void UpdateDataColumnProperty(RespondentData respondentData, string propertyName, JsonElement propertyValue)
        {
            if (IsDataColumn(propertyName) == false)
                throw new ArgumentException($"Property '{propertyName}' is not a recognized data column.");

            switch (propertyName)
            {
                case "Collector ID":
                    respondentData.CollectorId = propertyValue.GetUInt64().ToString();
                    break;

                case "Respondent ID":
                    respondentData.RespondentId = propertyValue.GetUInt64().ToString();
                    break;

                case "Start Date":
                    respondentData.SurveyStart = propertyValue.GetDateTime();
                    break;

                case "End Date":
                    respondentData.SurveyEnd = propertyValue.GetDateTime();
                    break;

                case "IP Address":
                    respondentData.IPAddress = propertyValue.GetString();
                    break;

                case "surveyName":
                    respondentData.SurveyName = propertyValue.GetString();
                    break;
            }
        }

        async Task AssignContactInfoValueToObject(RespondentData respondentData, string propertyName, string propertyValue)
        {
            var contactInformationType = await chatService.DetermineContactInformationType(propertyValue);

            switch (contactInformationType)
            {
                case ContactInformationType.Name:
                    if (string.IsNullOrEmpty(respondentData.FirstName))
                    {
                        respondentData.FirstName = propertyValue.Split(' ')[0];
                    }
                    
                    if (string.IsNullOrEmpty(respondentData.LastName))
                    {
                        respondentData.LastName = propertyValue.Substring(respondentData.FirstName.Length).Trim();
                    }
                    break;
                case ContactInformationType.EmailAddress:
                    respondentData.EmailAddress = propertyValue;
                    break;
                case ContactInformationType.PhoneNumber:
                    respondentData.PhoneNumber = propertyValue;
                    break;
            }
        }
    }
}
