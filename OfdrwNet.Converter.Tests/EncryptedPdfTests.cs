using System.IO;
using System.Threading.Tasks;
using Xunit;
using OfdrwNet.Converter;
using System.Xml.Linq;
using System.Linq;
using System;

namespace OfdrwNet.Converter.Tests
{
    public class EncryptedPdfTests
    {
        [Fact]
        public async Task PdfWithoutPassword_ShouldConvertSuccessfully()
        {
            // Test basic conversion without password (should work as before)
            var samplePdf = @"d:\workspace\ofdrw-master\ofdrw-net-specify\tests\fixtures\pdfs\simple_vector.pdf";
            Assert.True(File.Exists(samplePdf), $"Sample PDF not found: {samplePdf}");

            // Act
            var workDir = Environment.CurrentDirectory;
            var tempDir = Path.Combine(workDir, "test_temp", "encrypted_test_" + Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(tempDir);

            bool preserveTempDir = false;
            try
            {
                var options = new ConvertHelper.PdfToOfdOptions
                {
                    ExtractAndEmbedFonts = false,
                    ExtractText = false,
                    ExtractImage = false,
                    Password = null // No password
                };

                await ConvertHelper.PdfToOfdAsync(samplePdf, tempDir, options);

                // Assert
                var ofdXmlPath = Path.Combine(tempDir, "OFD.xml");
                Assert.True(File.Exists(ofdXmlPath), "OFD.xml should be created");

                Console.WriteLine("Non-encrypted PDF conversion test passed");
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
        public async Task PdfWithWrongPassword_ShouldStillWorkForNonEncryptedPdf()
        {
            // Test that providing a password for non-encrypted PDF still works (iText ignores it)
            var samplePdf = @"d:\workspace\ofdrw-master\ofdrw-net-specify\tests\fixtures\pdfs\simple_vector.pdf";
            Assert.True(File.Exists(samplePdf), $"Sample PDF not found: {samplePdf}");

            // Act
            var workDir = Environment.CurrentDirectory;
            var tempDir = Path.Combine(workDir, "test_temp", "wrong_password_test_" + Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(tempDir);

            bool preserveTempDir = false;
            try
            {
                var options = new ConvertHelper.PdfToOfdOptions
                {
                    ExtractAndEmbedFonts = false,
                    ExtractText = false,
                    ExtractImage = false,
                    Password = "wrongpassword" // Password provided but PDF is not encrypted
                };

                // This should work since the PDF is not encrypted
                await ConvertHelper.PdfToOfdAsync(samplePdf, tempDir, options);

                // Assert
                var ofdXmlPath = Path.Combine(tempDir, "OFD.xml");
                Assert.True(File.Exists(ofdXmlPath), "OFD.xml should be created");

                Console.WriteLine("Password provided for non-encrypted PDF test passed");
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
                        // Ignore cleanup errors
                    }
                }
            }
        }

        [Fact]
        public void PasswordSupport_ShouldBeAvailableInOptions()
        {
            // Test that the Password property exists and can be set
            var options = new ConvertHelper.PdfToOfdOptions
            {
                Password = "testpassword"
            };

            Assert.Equal("testpassword", options.Password);
            Console.WriteLine("Password property test passed");
        }
    }
}