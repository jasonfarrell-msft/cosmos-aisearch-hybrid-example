using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DTE.FunctionApp.Models
{
    public class PotentialQuestionData
    {
        public bool IsValidQuestion { get; set; }
        public string Question { get; set; } = string.Empty;
    }
}
