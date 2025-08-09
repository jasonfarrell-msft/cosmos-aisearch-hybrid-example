using System;
using System.IO;
using System.Threading.Tasks;
using System.Text.Json;
using OfficeOpenXml;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Extensions.Logging;

namespace DTE.FunctionApp.Services
{
    public class ExcelToJsonService : IExcelToJsonService
    {
        private readonly ILogger<ExcelToJsonService> _logger;

        public ExcelToJsonService(ILogger<ExcelToJsonService> logger)
        {
            _logger = logger;
            // Set EPPlus license context
            ExcelPackage.LicenseContext = LicenseContext.NonCommercial;
        }

        public Task<string> ConvertXlsxToJsonAsync(Stream xlsxStream, string fileName)
        {
            var data = new List<Dictionary<string, object?>>();

            using (var package = new ExcelPackage(xlsxStream))
            {
                var worksheet = package.Workbook.Worksheets.FirstOrDefault();
                if (worksheet == null)
                {
                    _logger.LogWarning($"No worksheets found in {fileName}");
                    return Task.FromResult(JsonSerializer.Serialize(data));
                }

                var rowCount = worksheet.Dimension?.Rows ?? 0;
                var colCount = worksheet.Dimension?.Columns ?? 0;

                if (rowCount == 0 || colCount == 0)
                {
                    _logger.LogWarning($"Empty worksheet in {fileName}");
                    return Task.FromResult(JsonSerializer.Serialize(data));
                }

                // Get headers from first row
                var headers = new List<string>();
                for (int col = 1; col <= colCount; col++)
                {
                    var headerValue = worksheet.Cells[1, col].Value?.ToString() ?? $"Column{col}";
                    headers.Add(headerValue);
                }

                // Process data rows
                for (int row = 2; row <= rowCount; row++)
                {
                    var rowData = new Dictionary<string, object?>();
                    bool hasData = false;

                    for (int col = 1; col <= colCount; col++)
                    {
                        var cell = worksheet.Cells[row, col];
                        var cellValue = cell.Value;
                        var header = headers[col - 1];

                        if (cellValue != null)
                        {
                            hasData = true;
                            
                            // Check if this is a date column and the cell contains a numeric value that could be a date
                            if (IsDateColumn(header) && cellValue is double)
                            {
                                try
                                {
                                    // Convert Excel date serial number to DateTime
                                    var dateValue = DateTime.FromOADate((double)cellValue);
                                    rowData[header] = dateValue.ToString("yyyy-MM-dd");
                                }
                                catch
                                {
                                    // If conversion fails, keep the original value
                                    rowData[header] = cellValue;
                                }
                            }
                            else
                            {
                                rowData[header] = cellValue;
                            }
                        }
                        else
                        {
                            rowData[header] = null;
                        }
                    }

                    // Only add rows that have at least some data and have a valid Respondent ID
                    data.Add(rowData);
                }

                // Filter out records where Respondent ID is null or empty
                var filteredData = data.Where(row => 
                {
                    // Check for various possible Respondent ID column names (case insensitive, without spaces)
                    var respondentIdKeys = row.Keys.Where(k => 
                        k.Equals("Respondent ID", StringComparison.OrdinalIgnoreCase) ||
                        k.Equals("Respondant ID", StringComparison.OrdinalIgnoreCase) ||
                        k.Equals("respondent Id", StringComparison.OrdinalIgnoreCase) ||
                        k.Equals("respondant Id", StringComparison.OrdinalIgnoreCase)
                    ).FirstOrDefault();

                    if (respondentIdKeys != null)
                    {
                        var respondentIdValue = row[respondentIdKeys];
                        return respondentIdValue != null && !string.IsNullOrWhiteSpace(respondentIdValue.ToString());
                    }

                    // If no Respondent ID column found, keep the record (assuming it's valid)
                    return true;
                }).ToList();

                _logger.LogInformation($"Processed {data.Count} rows from {worksheet.Name} worksheet in {fileName}. {filteredData.Count} rows remain after filtering out null Respondent IDs");
            
                var options = new JsonSerializerOptions
                {
                    WriteIndented = true,
                    PropertyNamingPolicy = JsonNamingPolicy.CamelCase
                };

                return Task.FromResult(JsonSerializer.Serialize(filteredData, options));
            }
        }

        private static bool IsDateColumn(string columnName)
        {
            var lowerColumnName = columnName.ToLowerInvariant();
            return lowerColumnName.Contains("date") || 
                   lowerColumnName.Contains("start") ||
                   lowerColumnName.Contains("end");
        }
    }
    
    public interface IExcelToJsonService
    {
        Task<string> ConvertXlsxToJsonAsync(Stream xlsxStream, string fileName);
    }
}
