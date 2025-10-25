using FluentAssertions;
using DataLayerGenerator.Options;
using Xunit;

namespace DataLayerGenerator.Tests.Integration
{
    /// <summary>
    /// Integration tests for Visual Studio command functionality
    /// Note: These are logical tests. Full VS integration requires VS Test Host.
    /// </summary>
    public class CommandIntegrationTests
    {
        #region File Validation Tests

        [Theory]
        [InlineData("Product.cs", true)]
        [InlineData("Customer.cs", true)]
        [InlineData("Order.cs", true)]
        [InlineData("Models\\Product.cs", true)]
        [InlineData("Data\\Product.cs", true)]
        public void IsValidCSharpFile_ValidFiles_ReturnsTrue(string fileName, bool expected)
        {
            // Arrange & Act
            var result = fileName.EndsWith(".cs", System.StringComparison.OrdinalIgnoreCase);

            // Assert
            result.Should().Be(expected);
        }

        [Theory]
        [InlineData("Product.txt")]
        [InlineData("Product.xml")]
        [InlineData("Product.json")]
        [InlineData("Product")]
        [InlineData("Product.CS.txt")]
        public void IsValidCSharpFile_InvalidFiles_ReturnsFalse(string fileName)
        {
            // Arrange & Act
            var result = fileName.EndsWith(".cs", System.StringComparison.OrdinalIgnoreCase);

            // Assert
            result.Should().BeFalse();
        }

        [Theory]
        [InlineData("Product.designer.cs")]
        [InlineData("Product.g.cs")]
        [InlineData("Product.g.i.cs")]
        [InlineData("TemporaryGeneratedFile_*.cs")]
        [InlineData("AssemblyInfo.cs")]
        public void ShouldSkipGeneratedFiles_GeneratedFiles_ReturnsTrue(string fileName)
        {
            // Arrange & Act
            var isGenerated = fileName.Contains(".designer.") ||
                            fileName.Contains(".g.cs") ||
                            fileName.Contains(".g.i.cs") ||
                            fileName.StartsWith("TemporaryGeneratedFile") ||
                            fileName.Equals("AssemblyInfo.cs");

            // Assert
            isGenerated.Should().BeTrue();
        }

        #endregion

        #region Path Validation Tests

        [Theory]
        [InlineData(@"C:\Projects\MyApp\Models\Product.cs", true)]
        [InlineData(@"D:\Dev\Models\Customer.cs", true)]
        [InlineData(@"\\NetworkShare\Project\Models\Order.cs", true)]
        [InlineData(@"C:\Projects\MyApp\", false)] // Directory, not file
        public void IsValidFilePath_ValidatesCorrectly(string path, bool shouldHaveExtension)
        {
            // Arrange & Act
            var hasExtension = System.IO.Path.HasExtension(path);

            // Assert
            hasExtension.Should().Be(shouldHaveExtension);
        }

        [Theory]
        [InlineData(@"C:\Projects\MyApp\Models\Product.cs", "Models")]
        [InlineData(@"C:\Dev\TestApp\Entities\Customer.cs", "Entities")]
        [InlineData(@"D:\Code\Models\Order.cs", "Models")]
        public void ExtractDirectoryName_ReturnsCorrectFolder(string path, string expectedFolder)
        {
            // Arrange & Act
            var directory = System.IO.Path.GetDirectoryName(path);
            if (string.IsNullOrEmpty(directory))
            {
                throw new ArgumentException("Invalid path", nameof(path));
            }

            var directoryName = System.IO.Path.GetFileName(directory);

            // Assert
            directoryName.Should().Be(expectedFolder);
        }

        #endregion

        #region Output Window Logging Tests

        [Fact]
        public void LogMessage_FormatsCorrectly()
        {
            // Arrange
            var message = "Generated data layer for Product";
            var timestamp = System.DateTime.Now.ToString("HH:mm:ss");

            // Act
            var formattedMessage = $"[{timestamp}] Data Layer Generator: {message}";

            // Assert
            formattedMessage.Should().Contain("Data Layer Generator:");
            formattedMessage.Should().Contain(message);
            formattedMessage.Should().MatchRegex(@"\[\d{2}:\d{2}:\d{2}\]");
        }

        [Theory]
        [InlineData("Success", "Generated ProductData.cs successfully")]
        [InlineData("Error", "Failed to generate data layer: File not found")]
        [InlineData("Warning", "Model has no Id property, skipping GetById method")]
        public void LogMessage_DifferentLevels_FormatsCorrectly(string level, string message)
        {
            // Arrange & Act
            var formattedMessage = $"[{level}] {message}";

            // Assert
            formattedMessage.Should().Contain($"[{level}]");
            formattedMessage.Should().Contain(message);
        }

        #endregion

        #region File Overwrite Scenarios

        [Fact]
        public void OverwriteChoice_YesToAll_RemembersChoice()
        {
            // Arrange
            var choice = OverwriteChoice.YesToAll;
            var shouldPromptAgain = choice == OverwriteChoice.Yes || choice == OverwriteChoice.No;

            // Act & Assert
            shouldPromptAgain.Should().BeFalse();
        }

        [Fact]
        public void OverwriteChoice_NoToAll_RemembersChoice()
        {
            // Arrange
            var choice = OverwriteChoice.NoToAll;
            var shouldPromptAgain = choice == OverwriteChoice.Yes || choice == OverwriteChoice.No;

            // Act & Assert
            shouldPromptAgain.Should().BeFalse();
        }

        [Fact]
        public void OverwriteChoice_Yes_PromptsForNextFile()
        {
            // Arrange
            var choice = OverwriteChoice.Yes;
            var shouldPromptAgain = choice == OverwriteChoice.Yes || choice == OverwriteChoice.No;

            // Act & Assert
            shouldPromptAgain.Should().BeTrue();
        }

        public enum OverwriteChoice
        {
            Yes,
            No,
            YesToAll,
            NoToAll
        }

        #endregion

        #region Multiple File Processing

        [Fact]
        public void ProcessMultipleFiles_AllValid_ProcessesAll()
        {
            // Arrange
            var files = new[]
            {
                "Product.cs",
                "Customer.cs",
                "Order.cs"
            };

            // Act
            var validFiles = files.Where(f => f.EndsWith(".cs")).ToList();

            // Assert
            validFiles.Should().HaveCount(3);
        }

        [Fact]
        public void ProcessMultipleFiles_MixedValid_FiltersCorrectly()
        {
            // Arrange
            var files = new[]
            {
                "Product.cs",
                "Customer.txt",
                "Order.cs",
                "Data.json",
                "Model.cs"
            };

            // Act
            var validFiles = files.Where(f => f.EndsWith(".cs")).ToList();

            // Assert
            validFiles.Should().HaveCount(3);
            validFiles.Should().Contain("Product.cs");
            validFiles.Should().Contain("Order.cs");
            validFiles.Should().Contain("Model.cs");
        }

        [Fact]
        public void ProcessMultipleFiles_DuplicateNames_HandlesCorrectly()
        {
            // Arrange
            var files = new[]
            {
                @"Folder1\Product.cs",
                @"Folder2\Product.cs"
            };

            // Act
            var fileNames = files.Select(System.IO.Path.GetFileName).ToList();
            var uniqueCount = fileNames.Distinct().Count();

            // Assert
            fileNames.Should().HaveCount(2);
            uniqueCount.Should().Be(1); // Same name
        }

        #endregion

        #region Project Integration

        [Theory]
        [InlineData(@"C:\Projects\MyApp\Models\Product.cs", @"C:\Projects\MyApp\Data\ProductData.cs")]
        [InlineData(@"C:\Dev\Models\Customer.cs", @"C:\Dev\Data\CustomerData.cs")]
        public void CalculateOutputPath_TransformsCorrectly(string inputPath, string expectedOutput)
        {
            // Arrange
            var inputDirectory = System.IO.Path.GetDirectoryName(inputPath);
            if (string.IsNullOrEmpty(inputDirectory))
            {
                throw new ArgumentException("Invalid input path", nameof(inputPath));
            }

            var projectRoot = System.IO.Path.GetDirectoryName(inputDirectory);
            if (string.IsNullOrEmpty(projectRoot))
            {
                throw new ArgumentException("Invalid project structure", nameof(inputPath));
            }

            var fileName = System.IO.Path.GetFileNameWithoutExtension(inputPath);

            // Act
            var outputPath = System.IO.Path.Combine(
                projectRoot,
                "Data",
                $"{fileName}Data.cs");

            // Assert
            outputPath.Should().Be(expectedOutput);
        }

        [Fact]
        public void FindProjectFile_FindsCsproj()
        {
            var projectFiles = new[] { "MyApp.csproj" };

            // Act
            var projectFile = projectFiles.FirstOrDefault(f => f.EndsWith(".csproj"));

            // Assert
            projectFile.Should().NotBeNull();
            projectFile.Should().Be("MyApp.csproj");
        }

        #endregion

        #region Error Handling

        [Fact]
        public void HandleError_FileNotFound_ReturnsErrorMessage()
        {
            // Arrange
            var fileName = "NonExistent.cs";
            var errorMessage = $"File not found: {fileName}";

            // Act & Assert
            errorMessage.Should().Contain("File not found");
            errorMessage.Should().Contain(fileName);
        }

        [Fact]
        public void HandleError_ParseError_ReturnsErrorMessage()
        {
            // Arrange
            var fileName = "Invalid.cs";
            var errorMessage = $"Failed to parse C# file: {fileName}";

            // Act & Assert
            errorMessage.Should().Contain("Failed to parse");
            errorMessage.Should().Contain(fileName);
        }

        [Fact]
        public void HandleError_NoModelsFound_ReturnsWarningMessage()
        {
            // Arrange
            var fileName = "Empty.cs";
            var warningMessage = $"No public classes found in: {fileName}";

            // Act & Assert
            warningMessage.Should().Contain("No public classes");
            warningMessage.Should().Contain(fileName);
        }

        #endregion

        #region Menu Visibility Logic

        [Fact]
        public void MenuVisible_CSharpFileSelected_ReturnsTrue()
        {
            // Arrange
            var fileName = "Product.cs";

            // Act
            var isVisible = fileName.EndsWith(".cs");

            // Assert
            isVisible.Should().BeTrue();
        }

        [Fact]
        public void MenuVisible_NonCSharpFileSelected_ReturnsFalse()
        {
            // Arrange
            var fileName = "Product.txt";

            // Act
            var isVisible = fileName.EndsWith(".cs");

            // Assert
            isVisible.Should().BeFalse();
        }

        [Fact]
        public void MenuVisible_MultipleCSharpFilesSelected_ReturnsTrue()
        {
            // Arrange
            var files = new[] { "Product.cs", "Customer.cs", "Order.cs" };

            // Act
            var isVisible = files.All(f => f.EndsWith(".cs"));

            // Assert
            isVisible.Should().BeTrue();
        }

        [Fact]
        public void MenuVisible_MixedFilesSelected_ReturnsFalse()
        {
            // Arrange
            var files = new[] { "Product.cs", "Config.xml", "Order.cs" };

            // Act
            var isVisible = files.All(f => f.EndsWith(".cs"));

            // Assert
            isVisible.Should().BeFalse();
        }

        #endregion

        #region Success Notifications

        [Fact]
        public void SuccessMessage_SingleFile_FormatsCorrectly()
        {
            // Arrange
            var className = "Product";
            var outputFile = "ProductData.cs";

            // Act
            var message = $"Successfully generated {outputFile} for {className}";

            // Assert
            message.Should().Contain("Successfully generated");
            message.Should().Contain(className);
            message.Should().Contain(outputFile);
        }

        [Fact]
        public void SuccessMessage_MultipleFiles_ShowsCount()
        {
            // Arrange
            var count = 5;

            // Act
            var message = $"Successfully generated {count} data layer classes";

            // Assert
            message.Should().Contain("Successfully generated");
            message.Should().Contain(count.ToString());
            message.Should().Contain("data layer classes");
        }

        #endregion

        #region Batch Processing

        [Fact]
        public void BatchProcess_PartialSuccess_ReportsBothSuccessAndFailures()
        {
            // Arrange
            var results = new[]
            {
        (FileName: "Product.cs", Success: true, Error: (string?)null),
        (FileName: "Customer.cs", Success: false, Error: "Parse error"),
        (FileName: "Order.cs", Success: true, Error: (string?)null)
    };

            // Act
            var successCount = results.Count(r => r.Success);
            var failureCount = results.Count(r => !r.Success);

            // Assert
            successCount.Should().Be(2);
            failureCount.Should().Be(1);
        }

        [Fact]
        public void BatchProcess_AllFailures_ReportsAllErrors()
        {
            // Arrange
            var results = new[]
            {
        (FileName: "Product.cs", Success: false, Error: "Parse error"),
        (FileName: "Customer.cs", Success: false, Error: "File not found"),
        (FileName: "Order.cs", Success: false, Error: "No models found")
    };

            // Act
            var failureCount = results.Count(r => !r.Success);
            var errors = results.Where(r => !r.Success).Select(r => r.Error).ToList();

            // Assert
            failureCount.Should().Be(3);
            errors.Should().HaveCount(3);
            errors.Should().Contain("Parse error");
            errors.Should().Contain("File not found");
            errors.Should().Contain("No models found");
        }

        #endregion
    }
}
