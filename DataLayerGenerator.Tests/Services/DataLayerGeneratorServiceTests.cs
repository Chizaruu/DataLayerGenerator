using FluentAssertions;
using DataLayerGenerator.Options;
using DataLayerGenerator.Services;
using DataLayerGenerator.Tests.Helpers;
using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Xunit;

namespace DataLayerGenerator.Tests.Services
{
    /// <summary>
    /// Tests for DataLayerGeneratorService
    /// </summary>
    public class DataLayerGeneratorServiceTests : IDisposable
    {
        private readonly string _tempDirectory;
        private bool _disposed;

        public DataLayerGeneratorServiceTests()
        {
            _tempDirectory = Path.Combine(Path.GetTempPath(), $"DataLayerTests_{Guid.NewGuid()}");
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
        public async Task AnalyzeModelsAsync_SimpleModel_ExtractsCorrectlyAsync()
        {
            // Arrange
            var service = new DataLayerGeneratorService();
            var filePath = CreateTempFile(TestHelpers.SampleModels.SimpleModel);

            // Act
            var result = await service.AnalyzeModelsAsync(filePath);

            // Assert
            result.Should().HaveCount(1);
            result[0].ClassName.Should().Be("Product");
            result[0].Namespace.Should().Be("TestApp.Models");
            result[0].HasIdProperty.Should().BeTrue();
            result[0].Properties.Should().HaveCount(3);
            result[0].Properties.Should().Contain(p => p.Name == "Id");
            result[0].Properties.Should().Contain(p => p.Name == "Name");
            result[0].Properties.Should().Contain(p => p.Name == "Price");
        }

        [Fact]
        public async Task AnalyzeModelsAsync_MultipleModels_ExtractsAllAsync()
        {
            // Arrange
            var service = new DataLayerGeneratorService();
            var filePath = CreateTempFile(TestHelpers.SampleModels.MultipleModels);

            // Act
            var result = await service.AnalyzeModelsAsync(filePath);

            // Assert
            result.Should().HaveCount(2); // Product and Category (InternalModel excluded by default)
            result.Should().Contain(m => m.ClassName == "Product");
            result.Should().Contain(m => m.ClassName == "Category");
            result.Should().NotContain(m => m.ClassName == "InternalModel");
        }

        [Fact]
        public async Task AnalyzeModelsAsync_ModelWithoutId_DetectedCorrectlyAsync()
        {
            // Arrange
            var service = new DataLayerGeneratorService();
            var filePath = CreateTempFile(TestHelpers.SampleModels.ModelWithoutId);

            // Act
            var result = await service.AnalyzeModelsAsync(filePath);

            // Assert
            result.Should().HaveCount(1);
            result[0].ClassName.Should().Be("Configuration");
            result[0].HasIdProperty.Should().BeFalse();
        }

        [Fact]
        public async Task AnalyzeModelsAsync_EmptyModel_ReturnsEmptyAsync()
        {
            // Arrange
            var service = new DataLayerGeneratorService();
            var filePath = CreateTempFile(TestHelpers.SampleModels.EmptyModel);

            // Act
            var result = await service.AnalyzeModelsAsync(filePath);

            // Assert
            result.Should().HaveCount(1);
            result[0].Properties.Should().BeEmpty();
        }

        [Fact]
        public async Task AnalyzeModelsAsync_NavigationProperties_DetectsCollectionsAsync()
        {
            // Arrange
            var service = new DataLayerGeneratorService();
            var filePath = CreateTempFile(TestHelpers.SampleModels.ModelWithNavigationProperties);

            // Act
            var result = await service.AnalyzeModelsAsync(filePath);

            // Assert
            var orderModel = result.FirstOrDefault(m => m.ClassName == "Order");
            orderModel.Should().NotBeNull();

            var orderItemsProperty = orderModel!.Properties.FirstOrDefault(p => p.Name == "OrderItems");
            orderItemsProperty.Should().NotBeNull();
            orderItemsProperty!.IsCollection.Should().BeTrue();

            var customerProperty = orderModel.Properties.FirstOrDefault(p => p.Name == "Customer");
            customerProperty.Should().NotBeNull();
            customerProperty!.IsCollection.Should().BeFalse();
        }

        [Fact]
        public void GenerateDataLayerClass_AllMethods_GeneratesCorrectly()
        {
            // Arrange
            var service = new DataLayerGeneratorService();
            var modelInfo = new ModelInfo
            {
                ClassName = "Product",
                Namespace = "TestApp.Models",
                HasIdProperty = true,
                Properties =
                [
                    new PropertyInfo { Name = "Id", Type = "int" },
                    new PropertyInfo { Name = "Name", Type = "string" },
                    new PropertyInfo { Name = "Price", Type = "decimal" }
                ]
            };

            var options = new DataLayerOptions
            {
                GenerateInterface = true,
                GenerateGetAll = true,
                GenerateGetById = true,
                GenerateAdd = true,
                GenerateUpdate = true,
                GenerateDelete = true
            };

            // Act
            var result = service.GenerateDataLayerClass("Product", modelInfo, options);

            // Assert
            result.Should().Contain("public class ProductData");
            result.Should().Contain("ApplicationDbContext context");
            result.Should().Contain("IProductData");
            result.Should().Contain("GetAllProducts()");
            result.Should().Contain("GetAllProductsAsync");
            result.Should().Contain("GetProductByIdAsync");
            result.Should().Contain("AddProductAsync");
            result.Should().Contain("UpdateProductAsync");
            result.Should().Contain("DeleteProductAsync");
        }

        [Fact]
        public void GenerateDataLayerClass_WithoutInterface_NoInterfaceReference()
        {
            // Arrange
            var service = new DataLayerGeneratorService();
            var modelInfo = new ModelInfo
            {
                ClassName = "Product",
                Namespace = "TestApp.Models",
                HasIdProperty = true
            };

            var options = new DataLayerOptions
            {
                GenerateInterface = false,
                GenerateGetAll = true
            };

            // Act
            var result = service.GenerateDataLayerClass("Product", modelInfo, options);

            // Assert
            result.Should().Contain("public class ProductData");
            result.Should().NotContain(": IProductData");
        }

        [Fact]
        public void GenerateDataLayerClass_SelectiveMethods_OnlyGeneratesSelected()
        {
            // Arrange
            var service = new DataLayerGeneratorService();
            var modelInfo = new ModelInfo
            {
                ClassName = "Product",
                Namespace = "TestApp.Models",
                HasIdProperty = true
            };

            var options = new DataLayerOptions
            {
                GenerateGetAll = true,
                GenerateGetById = false,
                GenerateAdd = true,
                GenerateUpdate = false,
                GenerateDelete = false
            };

            // Act
            var result = service.GenerateDataLayerClass("Product", modelInfo, options);

            // Assert
            result.Should().Contain("GetAllProducts");
            result.Should().Contain("AddProductAsync");
            result.Should().NotContain("GetProductByIdAsync");
            result.Should().NotContain("UpdateProductAsync");
            result.Should().NotContain("DeleteProductAsync");
        }

        [Fact]
        public void GenerateInterface_AllMethods_GeneratesCorrectly()
        {
            // Arrange
            var service = new DataLayerGeneratorService();
            var modelInfo = new ModelInfo
            {
                ClassName = "Customer",
                Namespace = "TestApp.Models",
                HasIdProperty = true
            };

            var options = new DataLayerOptions
            {
                GenerateInterface = true,
                GenerateGetAll = true,
                GenerateGetById = true,
                GenerateAdd = true,
                GenerateUpdate = true,
                GenerateDelete = true
            };

            // Act
            var result = service.GenerateInterface("Customer", modelInfo, options);

            // Assert
            result.Should().Contain("public interface ICustomerData");
            result.Should().Contain("IReadOnlyList<Customer> GetAllCustomers();");
            result.Should().Contain("Task<IReadOnlyList<Customer>> GetAllCustomersAsync");
            result.Should().Contain("Task<Customer?> GetCustomerByIdAsync");
            result.Should().Contain("Task AddCustomerAsync");
            result.Should().Contain("Task UpdateCustomerAsync");
            result.Should().Contain("Task DeleteCustomerAsync");
        }

        [Fact]
        public void GenerateInterface_WithoutGetById_NoGetByIdMethod()
        {
            // Arrange
            var service = new DataLayerGeneratorService();
            var modelInfo = new ModelInfo
            {
                ClassName = "Config",
                Namespace = "TestApp.Models",
                HasIdProperty = false
            };

            var options = new DataLayerOptions
            {
                GenerateGetAll = true,
                GenerateGetById = false,
                GenerateAdd = true
            };

            // Act
            var result = service.GenerateInterface("Config", modelInfo, options);

            // Assert
            result.Should().Contain("IReadOnlyList<Config> GetAllConfigs();");
            result.Should().NotContain("GetConfigByIdAsync");
        }

        [Fact]
        public void GenerateDataLayerClass_CustomDbContextName_UsesCorrectName()
        {
            // Arrange
            var options = new GeneratorOptions
            {
                DbContextName = "MyCustomDbContext"
            };
            var service = new DataLayerGeneratorService(options);

            var modelInfo = new ModelInfo
            {
                ClassName = "Product",
                Namespace = "TestApp.Models",
                HasIdProperty = true
            };

            var dataOptions = new DataLayerOptions
            {
                GenerateGetAll = true
            };

            // Act
            var result = service.GenerateDataLayerClass("Product", modelInfo, dataOptions);

            // Assert
            result.Should().Contain("MyCustomDbContext context");
            result.Should().Contain("context.Products");
        }

        [Fact]
        public void GenerateDataLayerClass_WithCustomQueries_IncludesPlaceholder()
        {
            // Arrange
            var service = new DataLayerGeneratorService();
            var modelInfo = new ModelInfo
            {
                ClassName = "Product",
                Namespace = "TestApp.Models",
                HasIdProperty = true
            };

            var options = new DataLayerOptions
            {
                GenerateCustomQueries = true
            };

            // Act
            var result = service.GenerateDataLayerClass("Product", modelInfo, options);

            // Assert
            result.Should().Contain("// TODO: Add custom query methods");
        }

        [Fact]
        public void GenerateDataLayerClass_ModelNamespace_TransformsToDataNamespace()
        {
            // Arrange
            var service = new DataLayerGeneratorService();
            var modelInfo = new ModelInfo
            {
                ClassName = "Product",
                Namespace = "MyCompany.ECommerce.Models",
                HasIdProperty = true
            };

            var options = new DataLayerOptions();

            // Act
            var result = service.GenerateDataLayerClass("Product", modelInfo, options);

            // Assert
            result.Should().Contain("namespace MyCompany.ECommerce.Data;");
            result.Should().Contain("using MyCompany.ECommerce.Models;");
        }

        [Fact]
        public async Task AnalyzeModelsAsync_RecordType_ExtractsCorrectlyAsync()
        {
            // Arrange
            var service = new DataLayerGeneratorService();
            var filePath = CreateTempFile(TestHelpers.SampleModels.RecordModel);

            // Act
            var result = await service.AnalyzeModelsAsync(filePath);

            // Assert
            result.Should().HaveCount(1);
            result[0].ClassName.Should().Be("Product");
            result[0].Properties.Should().HaveCount(3);
        }

        [Fact]
        public async Task AnalyzeModelsAsync_NullableProperties_HandlesCorrectlyAsync()
        {
            // Arrange
            var service = new DataLayerGeneratorService();
            var filePath = CreateTempFile(TestHelpers.SampleModels.ModelWithNullableProperties);

            // Act
            var result = await service.AnalyzeModelsAsync(filePath);

            // Assert
            result.Should().HaveCount(1);
            var person = result[0];
            person.Properties.Should().Contain(p => p.Name == "Age" && p.Type.Contains('?'));
            person.Properties.Should().Contain(p => p.Name == "BirthDate" && p.Type.Contains('?'));
        }

        [Fact]
        public void GenerateDataLayerClass_IncludesXmlDocumentation()
        {
            // Arrange
            var service = new DataLayerGeneratorService();
            var modelInfo = new ModelInfo
            {
                ClassName = "Product",
                Namespace = "TestApp.Models",
                HasIdProperty = true
            };

            var options = new DataLayerOptions
            {
                GenerateGetAll = true
            };

            // Act
            var result = service.GenerateDataLayerClass("Product", modelInfo, options);

            // Assert
            result.Should().Contain("/// <summary>");
            result.Should().Contain("/// Data access layer for Product entities");
            result.Should().Contain("/// Gets all Product entities");
        }

        [Fact]
        public void GenerateDataLayerClass_UsesModernCSharpSyntax()
        {
            // Arrange
            var service = new DataLayerGeneratorService();
            var modelInfo = new ModelInfo
            {
                ClassName = "Product",
                Namespace = "TestApp.Models",
                HasIdProperty = true
            };

            var options = new DataLayerOptions
            {
                GenerateGetAll = true
            };

            // Act
            var result = service.GenerateDataLayerClass("Product", modelInfo, options);

            // Assert
            // Primary constructor syntax
            result.Should().Contain("public class ProductData(ApplicationDbContext context)");

            // Collection expression
            result.Should().Contain("[.. context.Products");

            // Nullable reference type
            result.Should().Contain("Product?");

            // Default parameter
            result.Should().Contain("CancellationToken cancellationToken = default");
        }

        [Fact]
        public void GenerateDataLayerClass_IncludesAsNoTracking()
        {
            // Arrange
            var service = new DataLayerGeneratorService();
            var modelInfo = new ModelInfo
            {
                ClassName = "Product",
                Namespace = "TestApp.Models",
                HasIdProperty = true
            };

            var options = new DataLayerOptions
            {
                GenerateGetAll = true,
                GenerateGetById = true
            };

            // Act
            var result = service.GenerateDataLayerClass("Product", modelInfo, options);

            // Assert
            result.Should().Contain(".AsNoTracking()");
        }

        private string CreateTempFile(string content)
        {
            var fileName = $"Test_{Guid.NewGuid()}.cs";
            var filePath = Path.Combine(_tempDirectory, fileName);
            File.WriteAllText(filePath, content);
            return filePath;
        }
    }
}
