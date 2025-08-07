using System;
using System.IO;
using System.Text;

public static class FileInspector
{
    public static void InspectFile(string filePath)
    {
        Console.WriteLine($"\nInspecting file: {Path.GetFileName(filePath)}");
        
        try
        {
            using (var fs = new FileStream(filePath, FileMode.Open, FileAccess.Read))
            {
                // Read first 16 bytes to check file signature
                byte[] header = new byte[16];
                fs.Read(header, 0, 16);
                
                Console.WriteLine($"File size: {fs.Length} bytes");
                Console.WriteLine($"Header bytes: {BitConverter.ToString(header)}");
                
                // Check for various file signatures
                if (header[0] == 0x50 && header[1] == 0x4B) // PK
                {
                    Console.WriteLine("File signature: ZIP/Office Open XML (.xlsx)");
                }
                else if (header[0] == 0xD0 && header[1] == 0xCF && header[2] == 0x11 && header[3] == 0xE0)
                {
                    Console.WriteLine("File signature: OLE Compound Document (.xls/.doc)");
                }
                else if (header[0] == 0x09 && header[1] == 0x08)
                {
                    Console.WriteLine("File signature: Legacy Excel (.xls)");
                }
                else
                {
                    Console.WriteLine("File signature: Unknown/Custom format");
                    
                    // Look for text patterns that might indicate the real format
                    fs.Seek(0, SeekOrigin.Begin);
                    byte[] buffer = new byte[Math.Min(1024, fs.Length)];
                    fs.Read(buffer, 0, buffer.Length);
                    
                    string content = Encoding.ASCII.GetString(buffer);
                    if (content.Contains("Microsoft"))
                    {
                        Console.WriteLine("Contains 'Microsoft' string - likely Microsoft Office format");
                    }
                    if (content.Contains("Excel"))
                    {
                        Console.WriteLine("Contains 'Excel' string");
                    }
                    if (content.Contains("xl/"))
                    {
                        Console.WriteLine("Contains 'xl/' - likely corrupted .xlsx");
                    }
                }
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error inspecting file: {ex.Message}");
        }
    }
}
