using FluentAssertions;
using DataLayerGenerator.Options;
using DataLayerGenerator.Services;
using DataLayerGenerator.Tests.Helpers;
using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Xunit;

namespace DataLayerGenerator.Tests.Integration
{
    /// <summary>
    /// Integration tests for complete data layer generation workflows
    /// </summary>
    public class IntegrationTests : IDisposable
    {
        private readonly string _tempDirectory;
        private bool _disposed;

        public IntegrationTests()
        {
            _tempDirectory = Path.Combine(Path.GetTempPath(), $"IntegrationTests_{Guid.NewGuid()}");
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

        [Fact]
        public async Task CompleteWorkflow_SimpleModel_GeneratesBothFilesAsync()
        {
            // Arrange
            var service = new DataLayerGeneratorService();
            var modelFilePath = CreateTempFile(TestHelpers.SampleModels.SimpleModel, "Product.cs");

            // Act - Analyze model
            var models = await service.AnalyzeModelsAsync(modelFilePath);
            models.Should().HaveCount(1);

            var modelInfo = models[0];
            var options = new DataLayerOptions
            {
                GenerateInterface = true,
                GenerateGetAll = true,
                GenerateGetById = true,
                GenerateAdd = true,
                GenerateUpdate = true,
                GenerateDelete = true
            };

            // Generate data layer class
            var dataLayerCode = service.GenerateDataLayerClass("Product", modelInfo, options);
            var dataLayerPath = CreateTempFile(dataLayerCode, "ProductData.cs");

            // Generate interface
            var interfaceCode = service.GenerateInterface("Product", modelInfo, options);
            var interfacePath = CreateTempFile(interfaceCode, "IProductData.cs");

            // Assert
            File.Exists(dataLayerPath).Should().BeTrue();
            File.Exists(interfacePath).Should().BeTrue();

            var dataLayerContent = File.ReadAllText(dataLayerPath);
            dataLayerContent.Should().Contain("public class ProductData");
            dataLayerContent.Should().Contain("GetAllProductsAsync");
            dataLayerContent.Should().Contain("AddProductAsync");

            var interfaceContent = File.ReadAllText(interfacePath);
            interfaceContent.Should().Contain("public interface IProductData");
            interfaceContent.Should().Contain("Task<IReadOnlyList<Product>>");
        }

        [Fact]
        public async Task CompleteWorkflow_MultipleModels_GeneratesMultipleDataLayersAsync()
        {
            // Arrange
            var service = new DataLayerGeneratorService();
            var modelFilePath = CreateTempFile(TestHelpers.SampleModels.MultipleModels, "Models.cs");

            // Act
            var models = await service.AnalyzeModelsAsync(modelFilePath);

            // Generate data layers for each model
            var generatedFiles = new System.Collections.Generic.List<string>();

            foreach (var modelInfo in models)
            {
                var options = new DataLayerOptions
                {
                    GenerateGetAll = true,
                    GenerateAdd = true
                };

                var dataLayerCode = service.GenerateDataLayerClass(modelInfo.ClassName, modelInfo, options);
                var filePath = CreateTempFile(dataLayerCode, $"{modelInfo.ClassName}Data.cs");
                generatedFiles.Add(filePath);
            }

            // Assert
            generatedFiles.Should().HaveCount(2);
            generatedFiles.Should().OnlyContain(f => File.Exists(f));

            var productDataContent = File.ReadAllText(generatedFiles.First(f => f.Contains("ProductData")));
            productDataContent.Should().Contain("public class ProductData");

            var categoryDataContent = File.ReadAllText(generatedFiles.First(f => f.Contains("CategoryData")));
            categoryDataContent.Should().Contain("public class CategoryData");
        }

        [Fact]
        public async Task CompleteWorkflow_WithCustomOptions_RespectsOptionsAsync()
        {
            // Arrange
            var customOptions = new GeneratorOptions
            {
                DataLayerSuffix = "Repository",
                DbContextName = "MyDbContext",
                DataLayerNamespaceSuffix = ".Repositories",
                GenerateInterfaces = false
            };

            var service = new DataLayerGeneratorService(customOptions);
            var modelFilePath = CreateTempFile(TestHelpers.SampleModels.SimpleModel, "Product.cs");

            // Act
            var models = await service.AnalyzeModelsAsync(modelFilePath);
            var modelInfo = models[0];

            var dataOptions = new DataLayerOptions
            {
                GenerateInterface = false,
                GenerateGetAll = true,
                GenerateGetById = true
            };

            var dataLayerCode = service.GenerateDataLayerClass("Product", modelInfo, dataOptions);

            // Assert
            dataLayerCode.Should().Contain("public class ProductRepository");
            dataLayerCode.Should().Contain("MyDbContext context");
            dataLayerCode.Should().Contain("namespace TestApp.Repositories");
            dataLayerCode.Should().NotContain(": IProductRepository");
        }

        [Fact]
        public async Task CompleteWorkflow_SelectiveMethods_OnlyGeneratesSelectedAsync()
        {
            // Arrange
            var service = new DataLayerGeneratorService();
            var modelFilePath = CreateTempFile(TestHelpers.SampleModels.SimpleModel, "Product.cs");

            // Act
            var models = await service.AnalyzeModelsAsync(modelFilePath);
            var modelInfo = models[0];

            var options = new DataLayerOptions
            {
                GenerateGetAll = true,
                GenerateGetById = false,
                GenerateAdd = true,
                GenerateUpdate = false,
                GenerateDelete = false
            };

            var dataLayerCode = service.GenerateDataLayerClass("Product", modelInfo, options);
            var interfaceCode = service.GenerateInterface("Product", modelInfo, options);

            // Assert - Data layer
            dataLayerCode.Should().Contain("GetAllProductsAsync");
            dataLayerCode.Should().Contain("AddProductAsync");
            dataLayerCode.Should().NotContain("GetProductByIdAsync");
            dataLayerCode.Should().NotContain("UpdateProductAsync");
            dataLayerCode.Should().NotContain("DeleteProductAsync");

            // Assert - Interface
            interfaceCode.Should().Contain("GetAllProductsAsync");
            interfaceCode.Should().Contain("AddProductAsync");
            interfaceCode.Should().NotContain("GetProductByIdAsync");
            interfaceCode.Should().NotContain("UpdateProductAsync");
            interfaceCode.Should().NotContain("DeleteProductAsync");
        }

        [Fact]
        public async Task CompleteWorkflow_WithDocumentation_PreservesDocumentationAsync()
        {
            // Arrange
            var service = new DataLayerGeneratorService();
            var modelFilePath = CreateTempFile(TestHelpers.SampleModels.ModelWithDocumentation, "Customer.cs");

            // Act
            var models = await service.AnalyzeModelsAsync(modelFilePath);
            var modelInfo = models[0];

            var options = new DataLayerOptions
            {
                GenerateGetAll = true
            };

            var dataLayerCode = service.GenerateDataLayerClass("Customer", modelInfo, options);

            // Assert
            dataLayerCode.Should().Contain("/// <summary>");
            dataLayerCode.Should().Contain("/// Data access layer for Customer entities");
            dataLayerCode.Should().Contain("/// Gets all Customer entities");
        }

        [Fact]
        public async Task CompleteWorkflow_ComplexModel_HandlesNavigationPropertiesAsync()
        {
            // Arrange
            var service = new DataLayerGeneratorService();
            var modelFilePath = CreateTempFile(
                TestHelpers.SampleModels.ModelWithNavigationProperties,
                "Order.cs");

            // Act
            var models = await service.AnalyzeModelsAsync(modelFilePath);
            var orderModel = models.FirstOrDefault(m => m.ClassName == "Order");

            orderModel.Should().NotBeNull();

            var options = new DataLayerOptions
            {
                GenerateGetAll = true,
                GenerateGetById = true
            };

            var dataLayerCode = service.GenerateDataLayerClass("Order", orderModel!, options);

            // Assert
            dataLayerCode.Should().Contain("public class OrderData");
            dataLayerCode.Should().Contain("GetAllOrdersAsync");
            dataLayerCode.Should().Contain("GetOrderByIdAsync");
            dataLayerCode.Should().Contain("context.Orders");
        }

        [Fact]
        public async Task CompleteWorkflow_ModelWithoutId_SkipsGetByIdAsync()
        {
            // Arrange
            var service = new DataLayerGeneratorService();
            var modelFilePath = CreateTempFile(TestHelpers.SampleModels.ModelWithoutId, "Configuration.cs");

            // Act
            var models = await service.AnalyzeModelsAsync(modelFilePath);
            var modelInfo = models[0];

            modelInfo.HasIdProperty.Should().BeFalse();

            var options = new DataLayerOptions
            {
                GenerateGetAll = true,
                GenerateGetById = true, // Requested but should be skipped
                GenerateAdd = true
            };

            var dataLayerCode = service.GenerateDataLayerClass("Configuration", modelInfo, options);

            // Assert
            dataLayerCode.Should().Contain("GetAllConfigurationsAsync");
            dataLayerCode.Should().Contain("AddConfigurationAsync");
            // GetById and Delete should not be generated without Id property
            dataLayerCode.Should().NotContain("GetConfigurationByIdAsync");
        }

        [Fact]
        public async Task CompleteWorkflow_RecordType_GeneratesCorrectlyAsync()
        {
            // Arrange
            var service = new DataLayerGeneratorService();
            var modelFilePath = CreateTempFile(TestHelpers.SampleModels.RecordModel, "Product.cs");

            // Act
            var models = await service.AnalyzeModelsAsync(modelFilePath);
            models.Should().HaveCount(1);

            var modelInfo = models[0];
            var options = new DataLayerOptions
            {
                GenerateGetAll = true,
                GenerateAdd = true
            };

            var dataLayerCode = service.GenerateDataLayerClass("Product", modelInfo, options);

            // Assert
            dataLayerCode.Should().Contain("public class ProductData");
            dataLayerCode.Should().Contain("GetAllProductsAsync");
            dataLayerCode.Should().Contain("AddProductAsync");
        }

        [Fact]
        public async Task CompleteWorkflow_NamespaceTransformation_CorrectForAllCasesAsync()
        {
            // Arrange
            var testCases = new[]
            {
                ("MyApp.Models", "MyApp.Data"),
                ("Company.Project.Models", "Company.Project.Data"),
                ("Models", "Models.Data"), // Edge case
                ("MyNamespace", "MyNamespace.Data") // No .Models suffix
            };

            var service = new DataLayerGeneratorService();

            foreach (var (modelNamespace, expectedDataNamespace) in testCases)
            {
                var sourceCode = $@"
namespace {modelNamespace}
{{
    public class TestModel
    {{
        public int Id {{ get; set; }}
        public string Name {{ get; set; }}
    }}
}}";
                var modelFilePath = CreateTempFile(sourceCode, $"Test_{Guid.NewGuid()}.cs");

                // Act
                var models = await service.AnalyzeModelsAsync(modelFilePath);
                var modelInfo = models[0];

                var options = new DataLayerOptions { GenerateGetAll = true };
                var dataLayerCode = service.GenerateDataLayerClass("TestModel", modelInfo, options);

                // Assert
                dataLayerCode.Should().Contain($"namespace {expectedDataNamespace};");
            }
        }

        private string CreateTempFile(string content, string? fileName = null)
        {
            fileName ??= $"Test_{Guid.NewGuid()}.cs";
            var filePath = Path.Combine(_tempDirectory, fileName);
            File.WriteAllText(filePath, content);
            return filePath;
        }
    }
}
