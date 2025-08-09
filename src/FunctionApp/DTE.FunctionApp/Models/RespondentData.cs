using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DTE.FunctionApp.Models
{
    public class RespondentData
    {
        public string RespondentId { get; set; }
        public string CollectorId { get; set; }
        public string SurveyName { get; set; }
        public DateTime SurveyStart { get; set; }
        public DateTime SurveyEnd { get; set; }
        public string FirstName { get; set; }
        public string LastName { get; set; }

        public string Company { get; set; }
        public string EmailAddress { get; set; }
        public string PhoneNumber { get; set; }
        public string IPAddress { get; set; }
    }
}
