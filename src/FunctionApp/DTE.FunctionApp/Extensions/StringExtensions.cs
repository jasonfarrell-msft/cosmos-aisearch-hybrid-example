using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace DTE.FunctionApp.Extensions
{
    public static class StringExtensions
    {
        public static string ToDateString(this string str, string format)
        {
            if (DateTime.TryParse(str, out var dateTime))
            {
                return dateTime.ToString(format);
            }

            throw new FormatException($"Unable to convert '{str}' to a date.");
        }
    }
}