using DTE.FunctionApp.Extensions;
using DTE.FunctionApp.Models;
using System.Diagnostics;
using System.Text.Json;

namespace DTE.FunctionApp.Services
{
    public class QuestionAnswerDataService(IChatService chatService)
    {
        public async Task<IList<QuestionAnswerData>> GetQuestionAnswersAsync(JsonElement jsonData)
        {
            List<QuestionAnswerData> returnData = new();

            foreach (var property in jsonData.EnumerateObject())
            {
                string propertyName = property.Name;
                JsonElement propertyValue = property.Value;

                // we want to find a potential column that represents a user request
                (bool isValidQuestion, string questionText) = await CheckQuestion(propertyName);
                if (isValidQuestion)
                {
                    // if the user has provided a response, it will be the val of the property
                    // in some cases, the response can come on the next iteration. For that, we create a stub of the question
                    // in the last
                    returnData.Add(new QuestionAnswerData
                    {
                        Question = questionText,
                        Answer = propertyValue.GetValueAsString()
                    });
                }
                else
                {
                    // what we check here is, do we have a value, and does the last question added to
                    // the return have a null or empty answer.
                    var stringPropertyValue = propertyValue.GetValueAsString();
                    if (!string.IsNullOrEmpty(stringPropertyValue) && returnData.Any() && string.IsNullOrEmpty(returnData.Last().Answer))
                    {
                        // if so, we update the last question with the value
                        returnData.Last().Answer = stringPropertyValue;
                    }
                }
            }

            return returnData;
        }

        async Task<(bool, string)> CheckQuestion(string propertyName)
        {
            var isPotentialQuestion = propertyName.ToLower().StartsWith("column") == false
                && propertyName.Length > 20 
                && propertyName.IndexOf(' ') > 0;
            if (!isPotentialQuestion)
            {
                return (false, string.Empty);
            }

            // since this might be a question, we ask the chat service to determine if it is a question
            // the term "valid" is used here to indicate that it is a question we want to capture
            var data = await chatService.GetPotentialQuestionData(propertyName);
            return (data.IsValidQuestion, data.Question);
        }
    }
}
