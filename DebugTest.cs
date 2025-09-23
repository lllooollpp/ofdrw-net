using OfdrwNet.Converter;

public class DebugTest
{
    public static async Task Main(string[] args)
    {
        Console.WriteLine("Starting PDF to OFD conversion test...");
        try
        {
            string pdfPath = @"docs\test\test\0.pdf";
            string outDir = @"docs\test\test\0.ofd";

            if (!File.Exists(pdfPath))
            {
                Console.WriteLine($"Error: Input PDF file not found at '{pdfPath}'");
                return;
            }

            if (Directory.Exists(outDir))
            {
                Directory.Delete(outDir, true);
            }
            Directory.CreateDirectory(outDir);

            var options = new ConvertHelper.PdfToOfdOptions
            {
                // Logger will be created internally if not provided
                RealImageEmbedding = false, // 改为光栅化嵌入以避免色彩空间解码失败
            };

            await ConvertHelper.PdfToOfdAsync(pdfPath, outDir, options);

            Console.WriteLine("Conversion test completed successfully.");
            Console.WriteLine($"Output saved to: {Path.GetFullPath(outDir)}");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"An error occurred during conversion: {ex.ToString()}");
        }
    }
}
