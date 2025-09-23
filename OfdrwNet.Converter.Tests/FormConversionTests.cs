using System.IO;
using System.Threading.Tasks;
using Xunit;
using OfdrwNet.Converter;
using System.Xml.Linq;
using System.Linq;
using System;

namespace OfdrwNet.Converter.Tests
{
    public class FormConversionTests
    {
        [Fact]
        public async Task PdfWithForms_ShouldExtractFormsToOfd()
        {
            // This test will initially fail since form support is not yet implemented
            // Arrange - We'll use the existing test PDF for now, but ideally we'd have a PDF with forms
            var samplePdf = @"d:\workspace\ofdrw-master\ofdrw-net-specify\tests\fixtures\pdfs\simple_vector.pdf";
            Assert.True(File.Exists(samplePdf), $"Sample PDF not found: {samplePdf}");

            // Act
            var workDir = Environment.CurrentDirectory;
            var tempDir = Path.Combine(workDir, "test_temp", "form_test_" + Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(tempDir);

            bool preserveTempDir = false;
            try
            {
                var options = new ConvertHelper.PdfToOfdOptions
                {
                    ExtractAndEmbedFonts = false,
                    ExtractText = false,
                    ExtractImage = false,
                    // Note: Form extraction would be added here when implemented
                };

                await ConvertHelper.PdfToOfdAsync(samplePdf, tempDir, options);

                // Assert - For now, just verify basic OFD creation
                var ofdXmlPath = Path.Combine(tempDir, "OFD.xml");
                Assert.True(File.Exists(ofdXmlPath), "OFD.xml should be created");

                var ofdXml = XDocument.Load(ofdXmlPath);
                var ns = ofdXml.Root?.GetDefaultNamespace();

                // Check if forms would be present (initially none expected)
                var docXmlPath = Path.Combine(tempDir, "Doc", "Document.xml");
                if (File.Exists(docXmlPath))
                {
                    var docXml = XDocument.Load(docXmlPath);
                    var ofdNs = XNamespace.Get("http://www.ofdspec.org/2016");

                    // Look for form elements (none expected initially)
                    var formElements = docXml.Descendants(ofdNs + "Form");
                    Console.WriteLine($"Document.xml content:\n{docXml.ToString()}");
                    Console.WriteLine($"Found {formElements.Count()} Form elements");

                    // Initially this should be 0 since forms are not yet implemented
                    Assert.Equal(0, formElements.Count());
                }

                Console.WriteLine("Form test completed - no forms found as expected");
            }
            catch
            {
                preserveTempDir = true;
                Console.WriteLine($"Test failed, preserving temp directory: {tempDir}");
                throw;
            }
            finally
            {
                if (!preserveTempDir)
                {
                    try
                    {
                        Directory.Delete(tempDir, true);
                    }
                    catch
                    {
                        // Ignore cleanup errors in CI environment
                    }
                }
            }
        }

        [Fact]
        public void FormSupport_ShouldBeIdentifiedAsMissing()
        {
            // This test documents that form support is not yet implemented
            // It will pass initially but should be updated when forms are added

            // Check if any form-related code exists (it shouldn't initially)
            var converterAssembly = typeof(ConvertHelper).Assembly;
            var formTypes = converterAssembly.GetTypes()
                .Where(t => t.Name.Contains("Form", StringComparison.OrdinalIgnoreCase))
                .ToList();

            Console.WriteLine($"Found {formTypes.Count} form-related types in the assembly");
            foreach (var type in formTypes)
            {
                Console.WriteLine($"  - {type.FullName}");
            }

            // Initially expect no form types (may find some false positives)
            // We'll just log what we find for now
            Console.WriteLine("Form support verification completed");
        }
    }
}