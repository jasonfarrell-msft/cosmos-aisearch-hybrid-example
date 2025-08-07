using OfficeOpenXml;
using ExcelDataReader;
using Newtonsoft.Json;
using System.IO;
using System.Data;
using System.Text;
using DocumentFormat.OpenXml.Packaging;
using System.IO.Packaging;

class Program
{
    static void Main(string[] args)
    {
        // Set the license context for EPPlus (for non-commercial use)
        ExcelPackage.LicenseContext = LicenseContext.NonCommercial;
        
        // Register the code page provider for ExcelDataReader
        Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);
        
        try
        {
            // Get the files directory path
            string filesDirectory = Path.Combine(Directory.GetCurrentDirectory(), "files");
            
            if (!Directory.Exists(filesDirectory))
            {
                Console.WriteLine($"Files directory not found: {filesDirectory}");
                return;
            }
            
            // Get all .xlsx files in the files directory (not subdirectories)
            string[] excelFiles = Directory.GetFiles(filesDirectory, "*.xlsx", SearchOption.TopDirectoryOnly);
            
            if (excelFiles.Length == 0)
            {
                Console.WriteLine("No .xlsx files found in the files directory.");
                return;
            }
            
            Console.WriteLine($"Found {excelFiles.Length} Excel files to process.");
            
            // Dictionary to store header names and the files they appear in
            Dictionary<string, List<string>> headerToFiles = new Dictionary<string, List<string>>(StringComparer.OrdinalIgnoreCase);
            
            // Process each Excel file
            foreach (string filePath in excelFiles)
            {
                try
                {
                    Console.WriteLine($"Processing: {Path.GetFileName(filePath)}");
                    ProcessExcelFile(filePath, headerToFiles);
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Error processing {Path.GetFileName(filePath)}: {ex.Message}");
                }
            }
            
            // Create output structure with headers and their source files
            var headerInfo = headerToFiles
                .OrderBy(kvp => kvp.Key, StringComparer.OrdinalIgnoreCase)
                .Select(kvp => new
                {
                    Header = kvp.Key,
                    Files = kvp.Value.OrderBy(f => f, StringComparer.OrdinalIgnoreCase).ToList(),
                    FileCount = kvp.Value.Count
                })
                .ToList();
            
            // Create JSON object
            string json = JsonConvert.SerializeObject(headerInfo, Formatting.Indented);
            
            // Save to output.json
            string outputPath = Path.Combine(Directory.GetCurrentDirectory(), "output.json");
            File.WriteAllText(outputPath, json);
            
            Console.WriteLine($"Successfully extracted {headerInfo.Count} distinct headers from {excelFiles.Length} files.");
            Console.WriteLine($"Output saved to: {outputPath}");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"An error occurred: {ex.Message}");
        }
    }
    
    static void ProcessExcelFile(string filePath, Dictionary<string, List<string>> headerToFiles)
    {
        Exception? epPlusException = null;
        string? tempFilePath = null;
        string fileName = Path.GetFileName(filePath);
        
        try
        {
            // Try to remove any MIP/AIP labels that might be causing issues
            tempFilePath = TryRemoveLabelsAndCreateTemp(filePath);
            string fileToProcess = tempFilePath ?? filePath;
            
            // First, try with EPPlus for modern .xlsx files
            try
            {
                using (var package = new ExcelPackage(new FileInfo(fileToProcess)))
                {
                    ProcessWithEPPlus(package, headerToFiles, fileName);
                    return; // Success, no need to try ExcelDataReader
                }
            }
            catch (Exception ex)
            {
                epPlusException = ex;
                Console.WriteLine($"EPPlus failed for {Path.GetFileName(filePath)}: {ex.Message}");
                Console.WriteLine("Trying with ExcelDataReader...");
            }
            
            // If EPPlus fails, try with ExcelDataReader for older formats or OLE compound documents
            try
            {
                using (var stream = File.Open(fileToProcess, FileMode.Open, FileAccess.Read))
                {
                    using (var reader = ExcelReaderFactory.CreateReader(stream))
                    {
                        ProcessWithExcelDataReader(reader, headerToFiles, fileName);
                    }
                }
            }
            catch (Exception ex)
            {
                throw new Exception($"Both EPPlus and ExcelDataReader failed. EPPlus: {epPlusException?.Message}, ExcelDataReader: {ex.Message}");
            }
        }
        finally
        {
            // Clean up temporary file if created
            if (tempFilePath != null && File.Exists(tempFilePath))
            {
                try
                {
                    File.Delete(tempFilePath);
                }
                catch
                {
                    // Ignore cleanup errors
                }
            }
        }
    }
    
    static string? TryRemoveLabelsAndCreateTemp(string originalFilePath)
    {
        try
        {
            // Create a temporary file path
            string tempFilePath = Path.Combine(Path.GetTempPath(), $"temp_{Guid.NewGuid()}.xlsx");
            
            // Copy the original file to temp location
            File.Copy(originalFilePath, tempFilePath, true);
            
            // Try to open as an OpenXml package and remove any sensitivity labels
            try
            {
                using (var package = Package.Open(tempFilePath, FileMode.Open, FileAccess.ReadWrite))
                {
                    // Remove custom XML parts that might contain label information
                    var partsToRemove = new List<PackagePart>();
                    
                    foreach (var part in package.GetParts())
                    {
                        var uri = part.Uri.ToString();
                        // Remove Microsoft Information Protection related parts
                        if (uri.Contains("microsoft.com/office/2019/mip") ||
                            uri.Contains("customXml") ||
                            uri.Contains("MipLabelContent") ||
                            uri.Contains("MSIPLabelContent"))
                        {
                            partsToRemove.Add(part);
                        }
                    }
                    
                    foreach (var part in partsToRemove)
                    {
                        package.DeletePart(part.Uri);
                        Console.WriteLine($"Removed label part: {part.Uri}");
                    }
                }
                
                // Also try with DocumentFormat.OpenXml to remove any additional labeling
                try
                {
                    using (var document = SpreadsheetDocument.Open(tempFilePath, true))
                    {
                        // Remove any custom properties that might relate to labels
                        if (document.CustomFilePropertiesPart != null)
                        {
                            var properties = document.CustomFilePropertiesPart.Properties;
                            var propsToRemove = new List<DocumentFormat.OpenXml.OpenXmlElement>();
                            
                            foreach (var prop in properties.Elements())
                            {
                                // Check if this property might be related to labels
                                var propXml = prop.OuterXml;
                                if (propXml.Contains("MIP") || propXml.Contains("Label") || propXml.Contains("Sensitivity"))
                                {
                                    propsToRemove.Add(prop);
                                }
                            }
                            
                            foreach (var prop in propsToRemove)
                            {
                                prop.Remove();
                                Console.WriteLine($"Removed label-related property");
                            }
                        }
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Warning: Could not process with OpenXml: {ex.Message}");
                }
                
                return tempFilePath;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Warning: Could not remove labels from {Path.GetFileName(originalFilePath)}: {ex.Message}");
                
                // If we can't modify the file, delete the temp copy and return null
                if (File.Exists(tempFilePath))
                {
                    File.Delete(tempFilePath);
                }
                return null;
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Warning: Could not create temp file for {Path.GetFileName(originalFilePath)}: {ex.Message}");
            return null;
        }
    }
    
    static void ProcessWithEPPlus(ExcelPackage package, Dictionary<string, List<string>> headerToFiles, string fileName)
    {
        // Process each worksheet in the Excel file
        foreach (var worksheet in package.Workbook.Worksheets)
        {
            if (worksheet.Dimension == null || worksheet.Dimension.Rows == 0)
            {
                continue; // Skip empty worksheets
            }
            
            // Assume headers are in the first row
            int headerRow = 1;
            int lastColumn = worksheet.Dimension.End.Column;
            
            // Extract headers from the first row
            for (int col = 1; col <= lastColumn; col++)
            {
                var cellValue = worksheet.Cells[headerRow, col].Value;
                if (cellValue != null)
                {
                    string? headerName = cellValue.ToString()?.Trim();
                    if (!string.IsNullOrWhiteSpace(headerName))
                    {
                        AddHeaderToCollection(headerToFiles, headerName, fileName);
                    }
                }
            }
        }
    }
    
    static void ProcessWithExcelDataReader(IExcelDataReader reader, Dictionary<string, List<string>> headerToFiles, string fileName)
    {
        do
        {
            // Check if the sheet has any data
            if (reader.FieldCount == 0)
            {
                continue;
            }
            
            // Read the first row (headers)
            if (reader.Read())
            {
                for (int i = 0; i < reader.FieldCount; i++)
                {
                    var cellValue = reader.GetValue(i);
                    if (cellValue != null)
                    {
                        string? headerName = cellValue.ToString()?.Trim();
                        if (!string.IsNullOrWhiteSpace(headerName))
                        {
                            AddHeaderToCollection(headerToFiles, headerName, fileName);
                        }
                    }
                }
            }
        } while (reader.NextResult()); // Move to next worksheet
    }
    
    static void AddHeaderToCollection(Dictionary<string, List<string>> headerToFiles, string headerName, string fileName)
    {
        if (!headerToFiles.ContainsKey(headerName))
        {
            headerToFiles[headerName] = new List<string>();
        }
        
        if (!headerToFiles[headerName].Contains(fileName, StringComparer.OrdinalIgnoreCase))
        {
            headerToFiles[headerName].Add(fileName);
        }
    }
}
