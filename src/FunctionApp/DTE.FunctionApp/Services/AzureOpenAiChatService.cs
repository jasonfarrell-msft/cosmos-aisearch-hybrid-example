using Azure;
using Azure.AI.OpenAI;
using DTE.FunctionApp.Models;
using Microsoft.Extensions.Configuration;
using OpenAI.Chat;
using System.Text.Json;

namespace DTE.FunctionApp.Services
{
    public class AzureOpenAiChatService : IChatService
    {
        private readonly AzureOpenAIClient _client;

        public AzureOpenAiChatService(IConfiguration configuration)
        {
            _client = new AzureOpenAIClient(
                new Uri(configuration["AzureOpenAIEndpoint"]),
                new AzureKeyCredential(configuration["AzureOpenAIApiKey"])
            );
        }

        public async Task<ContactInformationType> DetermineContactInformationType(string value)
        {
            var rawContactInformationType = await GetContactInformationType(value);
            
            // Try to parse as direct enum name match first
            if (Enum.TryParse(rawContactInformationType.Replace(" ", ""), true, out ContactInformationType result))
            {
                return result;
            }
            
            // If no match found, default to first enum value
            return ContactInformationType.Unknown;
        }

        async Task<string> GetContactInformationType(string concatInfoPiece)
        {
            var chatClient = _client.GetChatClient("gpt-5-nano-deployment");
            var messages = new List<ChatMessage>
            {
                new SystemChatMessage(@"
                    You are a helpful assistant that determines the type of contact information provided. You are able to
                    classify contact information into the following categories:
                    - Name
                    - Address
                    - City
                    - State
                    - Zip
                    - Country
                    - Email Address
                    - Phone Number

                    Some data may not be in a format for the United States. Example, State could be a Canadian province, or Province could be a US state.
                    Also, the Zipcode may not be in a format for the United Statescould be a Canadian postal code or a UK postcode.

                    Respond with only the type of contact information that you determine from the provided value. If you are not able to determine the type, respond with 'Unknown'. Do not provide any additional information or context.
                "),
                new UserChatMessage($"Determine Contact Information Type for this: {concatInfoPiece}")
            };

            var completionResponse = await chatClient.CompleteChatAsync(messages);
            return completionResponse.Value.Content.FirstOrDefault()?.Text ?? string.Empty;
        }

        public async Task<PotentialQuestionData> GetPotentialQuestionData(string potentialQuestion)
        {
            var chatClient = _client.GetChatClient("gpt-5-nano-deployment");
            var messages = new List<ChatMessage>
            {
                new SystemChatMessage(@"
                    # Question Extraction Prompt
                    ## Instructions
                    Analyze the provided text and determine if it contains a question. Return the result as JSON according to the specifications below.

                    ## Rules
                    1. **Special Case - Company/Employment Questions:**
                       - If a question is asking about what company someone represents or works at, treat it as NOT a question
                       - Examples: ""What company do you work for?"", ""Which company do you represent?"", ""Who is your employer?""
                       - Return: `{""IsValidQuestion"": false}`

                    2. **If the text contains a question:**
                       - Look for sentences that end with question marks (?)
                       - Look for interrogative words like: who, what, when, where, why, how, do, does, did, can, could, would, will, should, are, is, etc.
                       - Consider implied questions even without question marks
                       - Extract the complete question sentence(s)

                    2. **If the text contains NO questions:**
                       - Return: `{""IsValidQuestion"": false}`

                    3. **If the text contains one or more questions (excluding company/employment questions):**
                       - Return: `{""IsValidQuestion"": true, ""Question"": ""[extracted question text]""}`
                       - If multiple questions exist, combine them or use the primary/first question

                    ## Output Format
                    - Return ONLY valid JSON
                    - No additional text or explanation
                    - Use double quotes for JSON strings
                    - Preserve the exact question text including punctuation

                    ## Examples

                    **Input:** ""Do you have a similar BSG system and are allowed to share the control knowledge (e.g., system functional requirements, logic)? If yes, please provide SCE with the information.""

                    **Output:** `{""IsValidQuestion"": true, ""Question"": ""Do you have a similar BSG system and are allowed to share the control knowledge (e.g., system functional requirements, logic)?""}`

                    **Input:** ""What company do you work for?""

                    **Output:** `{""IsValidQuestion"": false}`

                    **Input:** ""Please provide contact information""

                    **Output:** `{""IsValidQuestion"": false}`

                    **Input:** ""The system is working properly. No issues found.""

                    **Output:** `{""IsValidQuestion"": false}`
                "),
                new UserChatMessage(@$"
                    Analyze the following text and return the appropriate JSON structure:
                    {potentialQuestion}
                    Ensure the output is valid JSON and follows the specified format.
                ")};

            var completionResponse = await chatClient.CompleteChatAsync(messages);
            var textValue = completionResponse.Value.Content.FirstOrDefault()?.Text;
            if (string.IsNullOrEmpty(textValue))
                return null;            // this shouldnt ever happen, but just in case


            // convert to PotentialQuestionData
            return JsonSerializer.Deserialize<PotentialQuestionData>(textValue, new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            }) ?? new PotentialQuestionData();
        }
    }

    public interface IChatService
    {
        Task<ContactInformationType> DetermineContactInformationType(string value);
        Task<PotentialQuestionData> GetPotentialQuestionData(string potentialQuestion);
    }

    public enum ContactInformationType
    {
        Name,
        Unknown,
        Address,
        City,
        State,
        Zip,
        Country,
        EmailAddress,
        PhoneNumber
    }
}
