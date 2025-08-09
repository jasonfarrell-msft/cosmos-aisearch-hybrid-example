using System.Security.Cryptography;
using System.Text;

namespace DTE.FunctionApp.Models
{
    public class SurveyData
    {
        public string RespondentId { get; internal set; }
        public string SurveyId { get; internal set; }
        public string EmailAddress { get; internal set; }
        public string Name { get; internal set; }
        public string IPAddress { get; internal set; }
        public string PhoneNumber { get; internal set; }
        public string SurveyName { get; internal set; }
        public DateTime SurveyStart { get; internal set; }
        public DateTime SurveyEnd { get; internal set; }
        public string Question { get; internal set; }
        public string Answer { get; internal set; }

        public string id { get => GetDeterministicId(RespondentId, SurveyId, Question).ToString(); }

        private static Guid GetDeterministicId(string respondentId, string surveyId, string question)
        {
            if (string.IsNullOrEmpty(respondentId) || string.IsNullOrEmpty(surveyId))
                throw new InvalidOperationException("Missing RespondentId or SurveyId - cannot create EntryId");

            // Combine the IDs and question hash to create a unique string
            string questionHash = question != null ? question.GetHashCode().ToString() : "0";
            string combinedString = $"{respondentId}_{surveyId}_{questionHash}";
            
            // Use SHA256 to create a deterministic hash of the combined string
            using var sha256 = SHA256.Create();
            byte[] hashBytes = sha256.ComputeHash(Encoding.UTF8.GetBytes(combinedString));
            
            // Convert the first 16 bytes to a GUID format
            byte[] guidBytes = new byte[16];
            Array.Copy(hashBytes, guidBytes, 16);
            
            return new Guid(guidBytes);
        }
    }
}