using System.IO;
using System.Threading.Tasks;
using Xunit;
using OfdrwNet.Converter;
using System.Xml.Linq;
using System.Linq;
using System;

namespace OfdrwNet.Converter.Tests
{
    public class VectorConversionTests
    {
        [Fact]
        public async Task PdfWithSimpleVector_ShouldProduceOfdPath()
        {
            // Arrange
            var samplePdf = @"d:\workspace\ofdrw-master\ofdrw-net-specify\tests\fixtures\pdfs\simple_vector.pdf";
            Assert.True(File.Exists(samplePdf), $"Sample PDF not found: {samplePdf}");

            // Act - 使用工作目录的子目录
            var workDir = Environment.CurrentDirectory;
            var tempDir = Path.Combine(workDir, "test_temp", "ofd_test_" + Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(tempDir);

            bool preserveTempDir = false;
            try
            {
                await ConvertHelper.PdfToOfdAsync(samplePdf, tempDir);

                // Assert
                var ofdXmlPath = Path.Combine(tempDir, "OFD.xml");
                Assert.True(File.Exists(ofdXmlPath), "OFD.xml should be created");

                var ofdXml = XDocument.Load(ofdXmlPath);
                var ns = ofdXml.Root?.GetDefaultNamespace();
                var pathElements = ofdXml.Descendants(ns + "Path");
                
                // Debug: 输出OFD内容
                Console.WriteLine($"OFD.xml content:\n{ofdXml.ToString()}");
                Console.WriteLine($"Found {pathElements.Count()} Path elements");
                
                // 检查Document.xml
                var docXmlPath = Path.Combine(tempDir, "Doc", "Document.xml");
                if (File.Exists(docXmlPath))
                {
                    var docXml = XDocument.Load(docXmlPath);
                    var docPathElements = docXml.Descendants(ns + "PathObject");
                    Console.WriteLine($"Document.xml content:\n{docXml.ToString()}");
                    Console.WriteLine($"Found {docPathElements.Count()} PathObject elements in Document.xml");
                }
                else
                {
                    Console.WriteLine("Document.xml not found");
                }
                
                // 检查Content.xml
                var contentXmlPath = Path.Combine(tempDir, "Doc", "Pages", "Page_1", "Content.xml");
                if (File.Exists(contentXmlPath))
                {
                    var contentXml = XDocument.Load(contentXmlPath);
                    // 使用正确的命名空间
                    var ofdNs = XNamespace.Get("http://www.ofdspec.org/2016");
                    var contentPathElements = contentXml.Descendants(ofdNs + "PathObject");
                    Console.WriteLine($"Content.xml content:\n{contentXml.ToString()}");
                    Console.WriteLine($"Found {contentPathElements.Count()} PathObject elements in Content.xml");
                    
                    Assert.True(contentPathElements.Any(), "OFD should contain at least one PathObject element for vector graphics");

                    // 验证路径元素有正确的结构
                    foreach (var pathElement in contentPathElements)
                    {
                        var abbreviatedData = pathElement.Element(ofdNs + "AbbreviatedData");
                        Assert.NotNull(abbreviatedData);
                        Assert.False(string.IsNullOrWhiteSpace(abbreviatedData.Value), "AbbreviatedData should not be empty");
                    }
                }
                else
                {
                    Console.WriteLine("Content.xml not found");
                    Assert.True(false, "Content.xml should be created");
                }
            }
            catch
            {
                // 在失败时保留目录用于调试
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
    }
}
