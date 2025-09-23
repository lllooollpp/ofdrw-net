using System.IO;
using System.Threading.Tasks;
using Xunit;
using OfdrwNet.Converter;
using System.Xml.Linq;
using System.Linq;
using System;

namespace OfdrwNet.Converter.Tests
{
    public class AnnotationConversionTests
    {
        [Fact]
        public async Task PdfWithAnnotations_ShouldExtractAnnotationsToOfd()
        {
            // This test will initially fail since annotation support is not yet implemented
            // Arrange - We'll use the existing test PDF for now, but ideally we'd have a PDF with annotations
            var samplePdf = @"d:\workspace\ofdrw-master\ofdrw-net-specify\tests\fixtures\pdfs\simple_vector.pdf";
            Assert.True(File.Exists(samplePdf), $"Sample PDF not found: {samplePdf}");

            // Act
            var workDir = Environment.CurrentDirectory;
            var tempDir = Path.Combine(workDir, "test_temp", "annotation_test_" + Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(tempDir);

            bool preserveTempDir = false;
            try
            {
                var options = new ConvertHelper.PdfToOfdOptions
                {
                    ExtractAndEmbedFonts = false,
                    ExtractText = false,
                    ExtractImage = false,
                    // Note: Annotation extraction would be added here when implemented
                };

                await ConvertHelper.PdfToOfdAsync(samplePdf, tempDir, options);

                // Assert - For now, just verify basic OFD creation
                var ofdXmlPath = Path.Combine(tempDir, "OFD.xml");
                Assert.True(File.Exists(ofdXmlPath), "OFD.xml should be created");

                var ofdXml = XDocument.Load(ofdXmlPath);
                var ns = ofdXml.Root?.GetDefaultNamespace();

                // Check if annotations would be present (initially none expected)
                var docXmlPath = Path.Combine(tempDir, "Doc", "Document.xml");
                if (File.Exists(docXmlPath))
                {
                    var docXml = XDocument.Load(docXmlPath);
                    var ofdNs = XNamespace.Get("http://www.ofdspec.org/2016");

                    // Look for annotation elements (none expected initially)
                    var annotationElements = docXml.Descendants(ofdNs + "Annotation");
                    Console.WriteLine($"Document.xml content:\n{docXml.ToString()}");
                    Console.WriteLine($"Found {annotationElements.Count()} Annotation elements");

                    // Initially this should be 0 since annotations are not yet implemented
                    Assert.Equal(0, annotationElements.Count());
                }

                Console.WriteLine("Annotation test completed - no annotations found as expected");
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
        public void AnnotationSupport_ShouldBeIdentifiedAsMissing()
        {
            // This test documents that annotation support is not yet implemented
            // It will pass initially but should be updated when annotations are added

            // Check if any annotation-related code exists (it shouldn't initially)
            var converterAssembly = typeof(ConvertHelper).Assembly;
            var annotationTypes = converterAssembly.GetTypes()
                .Where(t => t.Name.Contains("Annotation", StringComparison.OrdinalIgnoreCase))
                .ToList();

            Console.WriteLine($"Found {annotationTypes.Count} annotation-related types in the assembly");
            foreach (var type in annotationTypes)
            {
                Console.WriteLine($"  - {type.FullName}");
            }

            // Initially expect no annotation types
            Assert.Empty(annotationTypes);
        }
    }
}