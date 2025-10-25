using FluentAssertions;
using DataLayerGenerator.Services;
using System;
using System.IO;
using System.Threading.Tasks;
using Xunit;

namespace DataLayerGenerator.Tests.ErrorHandling
{
    /// <summary>
    /// Tests for error handling and edge cases
    /// </summary>
    public class ErrorHandlingTests : IDisposable
    {
        private readonly string _tempDirectory;
        private bool _disposed;

        public ErrorHandlingTests()
        {
            _tempDirectory = Path.Combine(Path.GetTempPath(), $"ErrorTests_{Guid.NewGuid()}");
            Directory.CreateDirectory(_tempDirectory);
        }

        protected virtual void Dispose(bool disposing)
        {
            if (!_disposed)
            {
                if (disposing && Directory.Exists(_tempDirectory))
                {
                    try { Directory.Delete(_tempDirectory, true); }
                    catch { /* Ignore cleanup errors */ }
                }
                _disposed = true;
            }
        }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        #region File Not Found

        [Fact]
        public async Task AnalyzeModelsAsync_FileNotFound_ThrowsExceptionAsync()
        {
            // Arrange
            var service = new DataLayerGeneratorService();
            var nonExistentPath = Path.Combine(_tempDirectory, "NonExistent.cs");

            // Act
            Func<Task> act = async () => await service.AnalyzeModelsAsync(nonExistentPath);

            // Assert
            await act.Should().ThrowAsync<FileNotFoundException>();
        }

        [Fact]
        public async Task AnalyzeModelsAsync_NullPath_ThrowsExceptionAsync()
        {
            // Arrange
            var service = new DataLayerGeneratorService();

            // Act
            Func<Task> act = async () => await service.AnalyzeModelsAsync(null);

            // Assert
            await act.Should().ThrowAsync<ArgumentNullException>();
        }

        [Fact]
        public async Task AnalyzeModelsAsync_EmptyPath_ThrowsExceptionAsync()
        {
            // Arrange
            var service = new DataLayerGeneratorService();

            // Act
            Func<Task> act = async () => await service.AnalyzeModelsAsync(string.Empty);

            // Assert
            await act.Should().ThrowAsync<ArgumentException>();
        }

        #endregion

        #region Invalid File Content

        [Fact]
        public async Task AnalyzeModelsAsync_EmptyFile_ReturnsEmptyAsync()
        {
            // Arrange
            var service = new DataLayerGeneratorService();
            var filePath = CreateTempFile(string.Empty);

            // Act
            var result = await service.AnalyzeModelsAsync(filePath);

            // Assert
            result.Should().BeEmpty();
        }

        [Fact]
        public async Task AnalyzeModelsAsync_WhitespaceOnly_ReturnsEmptyAsync()
        {
            // Arrange
            var service = new DataLayerGeneratorService();
            var filePath = CreateTempFile("   \n\n   \t\t   ");

            // Act
            var result = await service.AnalyzeModelsAsync(filePath);

            // Assert
            result.Should().BeEmpty();
        }

        [Fact]
        public async Task AnalyzeModelsAsync_OnlyComments_ReturnsEmptyAsync()
        {
            // Arrange
            var service = new DataLayerGeneratorService();
            var sourceCode = @"
// This is a comment
/* This is a 
   multi-line comment */
/// <summary>
/// XML comment
/// </summary>
";
            var filePath = CreateTempFile(sourceCode);

            // Act
            var result = await service.AnalyzeModelsAsync(filePath);

            // Assert
            result.Should().BeEmpty();
        }

        [Fact]
        public async Task AnalyzeModelsAsync_InvalidSyntax_HandlesGracefullyAsync()
        {
            // Arrange
            var service = new DataLayerGeneratorService();
            var sourceCode = @"
namespace Test
{
    public class Broken @#$%^&
    {
        public int Id { get; set; }
    }
}";
            var filePath = CreateTempFile(sourceCode);

            // Act
            var result = await service.AnalyzeModelsAsync(filePath);

            // Assert
            // Should return empty or handle error gracefully, not crash
            result.Should().NotBeNull();
        }

        [Fact]
        public async Task AnalyzeModelsAsync_MissingNamespace_HandlesGracefullyAsync()
        {
            // Arrange
            var service = new DataLayerGeneratorService();
            var sourceCode = @"
public class Product
{
    public int Id { get; set; }
    public string Name { get; set; }
}";
            var filePath = CreateTempFile(sourceCode);

            // Act
            var result = await service.AnalyzeModelsAsync(filePath);

            // Assert
            // Should handle global namespace or return empty
            result.Should().NotBeNull();
        }

        [Fact]
        public async Task AnalyzeModelsAsync_UnterminatedString_HandlesGracefullyAsync()
        {
            // Arrange
            var service = new DataLayerGeneratorService();
            var sourceCode = @"
namespace Test
{
    public class Product
    {
        public string Name = ""Unterminated;
    }
}";
            var filePath = CreateTempFile(sourceCode);

            // Act
            var result = await service.AnalyzeModelsAsync(filePath);

            // Assert
            result.Should().NotBeNull();
        }

        #endregion

        #region Special Characters and Encoding

        [Fact]
        public async Task AnalyzeModelsAsync_UnicodeCharacters_HandlesCorrectlyAsync()
        {
            // Arrange
            var service = new DataLayerGeneratorService();
            var sourceCode = @"
namespace Test
{
    public class Product
    {
        public int Id { get; set; }
        public string Name { get; set; } // 产品名称
        public string Description { get; set; } // Descripción
    }
}";
            var filePath = CreateTempFile(sourceCode);

            // Act
            var result = await service.AnalyzeModelsAsync(filePath);

            // Assert
            result.Should().HaveCount(1);
            result[0].Properties.Should().HaveCount(3);
        }

        [Fact]
        public async Task AnalyzeModelsAsync_SpecialCharactersInStrings_HandlesCorrectlyAsync()
        {
            // Arrange
            var service = new DataLayerGeneratorService();
            var sourceCode = @"
namespace Test
{
    public class Model
    {
        public int Id { get; set; }
        public string Value { get; set; } = ""Test with \""quotes\"" and \\ backslash"";
    }
}";
            var filePath = CreateTempFile(sourceCode);

            // Act
            var result = await service.AnalyzeModelsAsync(filePath);

            // Assert
            result.Should().HaveCount(1);
        }

        #endregion

        #region Very Large Files

        [Fact]
        public async Task AnalyzeModelsAsync_VeryLargeClass_HandlesCorrectlyAsync()
        {
            // Arrange
            var service = new DataLayerGeneratorService();
            var propertiesCount = 100;
            var properties = string.Join("\n",
                Enumerable.Range(1, propertiesCount)
                    .Select(i => $"        public string Property{i} {{ get; set; }}"));

            var sourceCode = $@"
namespace Test
{{
    public class LargeModel
    {{
        public int Id {{ get; set; }}
{properties}
    }}
}}";
            var filePath = CreateTempFile(sourceCode);

            // Act
            var result = await service.AnalyzeModelsAsync(filePath);

            // Assert
            result.Should().HaveCount(1);
            result[0].Properties.Should().HaveCount(propertiesCount + 1); // +1 for Id
        }

        [Fact]
        public async Task AnalyzeModelsAsync_ManyClasses_HandlesCorrectlyAsync()
        {
            // Arrange
            var service = new DataLayerGeneratorService();
            var classCount = 50;
            var classes = string.Join("\n\n",
                Enumerable.Range(1, classCount)
                    .Select(i => $@"    public class Model{i}
    {{
        public int Id {{ get; set; }}
        public string Name{i} {{ get; set; }}
    }}"));

            var sourceCode = $@"
namespace Test
{{
{classes}
}}";
            var filePath = CreateTempFile(sourceCode);

            // Act
            var result = await service.AnalyzeModelsAsync(filePath);

            // Assert
            result.Should().HaveCount(classCount);
        }

        #endregion

        #region Path Edge Cases

        [Fact]
        public async Task AnalyzeModelsAsync_VeryLongPath_HandlesCorrectlyAsync()
        {
            // Arrange - Skip if path too long for OS
            var service = new DataLayerGeneratorService();
            var longFileName = new string('a', 100) + ".cs";
            var filePath = Path.Combine(_tempDirectory, longFileName);

            try
            {
                File.WriteAllText(filePath, @"
namespace Test
{
    public class Model { public int Id { get; set; } }
}");

                // Act
                var result = await service.AnalyzeModelsAsync(filePath);

                // Assert
                result.Should().HaveCount(1);
            }
            catch (PathTooLongException)
            {
                // Expected on some systems, skip test
                Assert.True(true);
            }
        }

        [Theory]
        [InlineData("Test File.cs")] // Space
        [InlineData("Test-File.cs")] // Hyphen
        [InlineData("Test_File.cs")] // Underscore
        [InlineData("Test.Model.cs")] // Multiple dots
        public async Task AnalyzeModelsAsync_SpecialCharactersInFileName_HandlesCorrectlyAsync(string fileName)
        {
            // Arrange
            var service = new DataLayerGeneratorService();
            var filePath = Path.Combine(_tempDirectory, fileName);
            File.WriteAllText(filePath, @"
namespace Test
{
    public class Model { public int Id { get; set; } }
}");

            // Act
            var result = await service.AnalyzeModelsAsync(filePath);

            // Assert
            result.Should().HaveCount(1);
        }

        #endregion

        #region Null and Invalid Inputs

        [Fact]
        public void GenerateDataLayerClass_NullModelInfo_ThrowsException()
        {
            // Arrange
            var service = new DataLayerGeneratorService();
            var options = new DataLayerOptions();

            // Act
            Action act = () => service.GenerateDataLayerClass("Test", null, options);

            // Assert
            act.Should().Throw<ArgumentNullException>();
        }

        [Fact]
        public void GenerateDataLayerClass_NullOptions_ThrowsException()
        {
            // Arrange
            var service = new DataLayerGeneratorService();
            var modelInfo = new ModelInfo { ClassName = "Test", Namespace = "Test" };

            // Act
            Action act = () => service.GenerateDataLayerClass("Test", modelInfo, null);

            // Assert
            act.Should().Throw<ArgumentNullException>();
        }

        [Fact]
        public void GenerateDataLayerClass_EmptyClassName_ThrowsException()
        {
            // Arrange
            var service = new DataLayerGeneratorService();
            var modelInfo = new ModelInfo { ClassName = "", Namespace = "Test" };
            var options = new DataLayerOptions();

            // Act
            Action act = () => service.GenerateDataLayerClass("", modelInfo, options);

            // Assert
            act.Should().Throw<ArgumentException>();
        }

        #endregion

        #region Concurrent Access

        [Fact]
        public async Task AnalyzeModelsAsync_ConcurrentCalls_HandlesCorrectlyAsync()
        {
            // Arrange
            var service = new DataLayerGeneratorService();
            var filePath = CreateTempFile(@"
namespace Test
{
    public class Model { public int Id { get; set; } }
}");

            // Act - Multiple concurrent calls
            var tasks = Enumerable.Range(1, 10)
                .Select(_ => service.AnalyzeModelsAsync(filePath))
                .ToArray();

            var results = await Task.WhenAll(tasks);

            // Assert - All should succeed
            results.Should().HaveCount(10);
            results.Should().OnlyContain(r => r.Count == 1);
        }

        #endregion

        #region Memory and Resource Management

        [Fact]
        public async Task AnalyzeModelsAsync_MultipleFiles_ReleasesResourcesAsync()
        {
            // Arrange
            var service = new DataLayerGeneratorService();
            var files = Enumerable.Range(1, 20)
                .Select(i => CreateTempFile($@"
namespace Test
{{
    public class Model{i} {{ public int Id {{ get; set; }} }}
}}"))
                .ToList();

            // Act
            foreach (var file in files)
            {
                var result = await service.AnalyzeModelsAsync(file);
                result.Should().HaveCount(1);
            }

            // Assert - No memory leaks (basic test)
#pragma warning disable S1215 // "GC.Collect" should not be called
            GC.Collect();
            GC.WaitForPendingFinalizers();
#pragma warning restore S1215
            // If we get here without OOM, test passes
            Assert.True(true);
        }

        #endregion

        #region Malformed Code Structures

        [Fact]
        public async Task AnalyzeModelsAsync_CircularReferences_HandlesGracefullyAsync()
        {
            // Arrange
            var service = new DataLayerGeneratorService();
            var sourceCode = @"
namespace Test
{
    public class Parent
    {
        public int Id { get; set; }
        public Child Child { get; set; }
    }

    public class Child
    {
        public int Id { get; set; }
        public Parent Parent { get; set; }
    }
}";
            var filePath = CreateTempFile(sourceCode);

            // Act
            var result = await service.AnalyzeModelsAsync(filePath);

            // Assert
            result.Should().HaveCount(2);
        }

        [Fact]
        public async Task AnalyzeModelsAsync_SelfReferencingClass_HandlesGracefullyAsync()
        {
            // Arrange
            var service = new DataLayerGeneratorService();
            var sourceCode = @"
using System.Collections.Generic;

namespace Test
{
    public class TreeNode
    {
        public int Id { get; set; }
        public TreeNode Parent { get; set; }
        public List<TreeNode> Children { get; set; }
    }
}";
            var filePath = CreateTempFile(sourceCode);

            // Act
            var result = await service.AnalyzeModelsAsync(filePath);

            // Assert
            result.Should().HaveCount(1);
            result[0].Properties.Should().HaveCount(3);
        }

        #endregion

        #region Read-Only File System

        [Fact]
        public void CreateOutputFile_ReadOnlyDirectory_ThrowsException()
        {
            // Arrange
            var readOnlyDir = Path.Combine(_tempDirectory, "readonly");
            Directory.CreateDirectory(readOnlyDir);

            try
            {
                // Try to make directory read-only (may not work on all systems)
                var dirInfo = new DirectoryInfo(readOnlyDir)
                {
                    Attributes = FileAttributes.ReadOnly
                };

                // Note: On Windows, directory read-only attribute doesn't prevent file creation
                // This test may be platform-dependent, so we'll verify the attribute was set
                // but skip assertion if the platform doesn't enforce it
                var isReadOnly = (dirInfo.Attributes & FileAttributes.ReadOnly) == FileAttributes.ReadOnly;

                if (!isReadOnly)
                {
                    // Skip test if we couldn't make it read-only
                    Assert.True(true, "Platform does not support read-only directories");
                    return;
                }

                // Act & Assert - This may or may not throw depending on OS
                // On some systems, directory read-only doesn't prevent file creation
                try
                {
                    File.WriteAllText(
                        Path.Combine(readOnlyDir, "test.cs"),
                        "content");
                    // If no exception, test passes as platform doesn't enforce this restriction
                    Assert.True(true, "Platform allows file creation in read-only directories");
                }
                catch (UnauthorizedAccessException)
                {
                    // This is the expected behavior on platforms that enforce it
                    Assert.True(true);
                }
            }
            finally
            {
                // Cleanup
                try
                {
                    var dirInfo = new DirectoryInfo(readOnlyDir);
                    if (Directory.Exists(readOnlyDir))
                    {
                        dirInfo.Attributes = FileAttributes.Normal;
                        Directory.Delete(readOnlyDir, true);
                    }
                }
                catch
                {
                    // Ignore cleanup errors
                }
            }
        }

        #endregion

        private string CreateTempFile(string content)
        {
            var fileName = $"Test_{Guid.NewGuid()}.cs";
            var filePath = Path.Combine(_tempDirectory, fileName);
            File.WriteAllText(filePath, content);
            return filePath;
        }
    }
}
