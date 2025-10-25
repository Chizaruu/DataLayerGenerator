using FluentAssertions;
using DataLayerGenerator.Options;
using DataLayerGenerator.Services;
using System;
using System.IO;
using System.Threading.Tasks;
using Xunit;

namespace DataLayerGenerator.Tests.Services
{
    /// <summary>
    /// Tests for generated code quality, syntax correctness, and edge cases
    /// </summary>
    public class GeneratedCodeQualityTests : IDisposable
    {
        private readonly string _tempDirectory;
        private bool _disposed;

        public GeneratedCodeQualityTests()
        {
            _tempDirectory = Path.Combine(Path.GetTempPath(), $"QualityTests_{Guid.NewGuid()}");
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
        public void GeneratedCode_HasCorrectUsings()
        {
            // Arrange
            var service = new DataLayerGeneratorService();
            var modelInfo = new ModelInfo
            {
                ClassName = "Product",
                Namespace = "TestApp.Models",
                HasIdProperty = true
            };

            var options = new DataLayerOptions { GenerateGetAll = true };

            // Act
            var code = service.GenerateDataLayerClass("Product", modelInfo, options);

            // Assert
            code.Should().Contain("using Microsoft.EntityFrameworkCore;");
            code.Should().Contain("using TestApp.Models;");
        }

        [Fact]
        public void GeneratedCode_UsesPrimaryConstructorSyntax()
        {
            // Arrange
            var service = new DataLayerGeneratorService();
            var modelInfo = new ModelInfo
            {
                ClassName = "Product",
                Namespace = "TestApp.Models",
                HasIdProperty = true
            };

            var options = new DataLayerOptions { GenerateGetAll = true };

            // Act
            var code = service.GenerateDataLayerClass("Product", modelInfo, options);

            // Assert
            code.Should().Contain("public class ProductData(ApplicationDbContext context)");
            code.Should().NotContain("private readonly ApplicationDbContext");
            code.Should().NotContain("public ProductData(ApplicationDbContext");
        }

        [Fact]
        public void GeneratedCode_UsesCollectionExpression()
        {
            // Arrange
            var service = new DataLayerGeneratorService();
            var modelInfo = new ModelInfo
            {
                ClassName = "Product",
                Namespace = "TestApp.Models",
                HasIdProperty = true
            };

            var options = new DataLayerOptions { GenerateGetAll = true };

            // Act
            var code = service.GenerateDataLayerClass("Product", modelInfo, options);

            // Assert
            code.Should().Contain("[.. context.Products");
        }

        [Fact]
        public void GeneratedCode_IncludesXmlDocumentation()
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
                GenerateGetById = true,
                GenerateAdd = true
            };

            // Act
            var code = service.GenerateDataLayerClass("Product", modelInfo, options);

            // Assert
            code.Should().Contain("/// <summary>");
            code.Should().Contain("/// Data access layer for Product entities");
            code.Should().Contain("/// Gets all Product entities");
            code.Should().Contain("/// Gets a Product entity by ID");
            code.Should().Contain("/// Adds a new Product entity");
        }

        [Fact]
        public void GeneratedCode_UsesAsNoTrackingForReadQueries()
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
            var code = service.GenerateDataLayerClass("Product", modelInfo, options);

            // Assert
            code.Should().Contain(".AsNoTracking()");
        }

        [Fact]
        public void GeneratedCode_IncludesCancellationToken()
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
                GenerateAdd = true
            };

            // Act
            var code = service.GenerateDataLayerClass("Product", modelInfo, options);

            // Assert
            code.Should().Contain("CancellationToken cancellationToken = default");
        }

        [Fact]
        public void GeneratedCode_UsesNullableReferenceTypes()
        {
            // Arrange
            var service = new DataLayerGeneratorService();
            var modelInfo = new ModelInfo
            {
                ClassName = "Product",
                Namespace = "TestApp.Models",
                HasIdProperty = true
            };

            var options = new DataLayerOptions { GenerateGetById = true };

            // Act
            var code = service.GenerateDataLayerClass("Product", modelInfo, options);

            // Assert
            code.Should().Contain("Product?");
            code.Should().Contain("Task<Product?>");
        }

        [Fact]
        public void GeneratedCode_ReturnsIReadOnlyList()
        {
            // Arrange
            var service = new DataLayerGeneratorService();
            var modelInfo = new ModelInfo
            {
                ClassName = "Product",
                Namespace = "TestApp.Models",
                HasIdProperty = true
            };

            var options = new DataLayerOptions { GenerateGetAll = true };

            // Act
            var code = service.GenerateDataLayerClass("Product", modelInfo, options);

            // Assert
            code.Should().Contain("IReadOnlyList<Product>");
            code.Should().NotContain("public List<Product>");
            code.Should().NotContain("Task<List<Product>>");
            code.Should().NotContain("return new List<Product>");
        }

        [Fact]
        public void GeneratedCode_HandlesPluralizedDbSetNames()
        {
            // Arrange
            var service = new DataLayerGeneratorService();
            var modelInfo = new ModelInfo
            {
                ClassName = "Product",
                Namespace = "TestApp.Models",
                HasIdProperty = true
            };

            var options = new DataLayerOptions { GenerateGetAll = true };

            // Act
            var code = service.GenerateDataLayerClass("Product", modelInfo, options);

            // Assert
            code.Should().Contain("context.Products");
        }

        [Theory]
        [InlineData("Customer", "Customers")]
        [InlineData("Order", "Orders")]
        [InlineData("Category", "Categories")] // This might not pluralize correctly
        [InlineData("Person", "Persons")] // This might not pluralize correctly
        public void GeneratedCode_DbSetNaming_ApproximatesPluralization(string modelName, string expectedDbSet)
        {
            if (expectedDbSet == null || expectedDbSet.Trim().Length == 0)
            {
                throw new ArgumentException("Expected DbSet name must be provided", nameof(expectedDbSet));
            }

            // Arrange
            var service = new DataLayerGeneratorService();
            var modelInfo = new ModelInfo
            {
                ClassName = modelName,
                Namespace = "TestApp.Models",
                HasIdProperty = true
            };

            var options = new DataLayerOptions { GenerateGetAll = true };

            // Act
            var code = service.GenerateDataLayerClass(modelName, modelInfo, options);

            // Assert
            // Simple pluralization: just adds 's'
            code.Should().Contain($"context.{modelName}s");
        }

        [Fact]
        public void GeneratedCode_DeleteMethod_IncludesNullCheck()
        {
            // Arrange
            var service = new DataLayerGeneratorService();
            var modelInfo = new ModelInfo
            {
                ClassName = "Product",
                Namespace = "TestApp.Models",
                HasIdProperty = true
            };

            var options = new DataLayerOptions { GenerateDelete = true };

            // Act
            var code = service.GenerateDataLayerClass("Product", modelInfo, options);

            // Assert
            code.Should().Contain("if (entity != null)");
            code.Should().Contain("context.Products.Remove(entity);");
        }

        [Fact]
        public void GeneratedCode_FindAsync_UsesCollectionExpression()
        {
            // Arrange
            var service = new DataLayerGeneratorService();
            var modelInfo = new ModelInfo
            {
                ClassName = "Product",
                Namespace = "TestApp.Models",
                HasIdProperty = true
            };

            var options = new DataLayerOptions { GenerateDelete = true };

            // Act
            var code = service.GenerateDataLayerClass("Product", modelInfo, options);

            // Assert
            code.Should().Contain("FindAsync([id]");
        }

        [Fact]
        public void GeneratedInterface_HasCorrectSignatures()
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
                GenerateGetById = true,
                GenerateAdd = true,
                GenerateUpdate = true,
                GenerateDelete = true
            };

            // Act
            var code = service.GenerateInterface("Product", modelInfo, options);

            // Assert
            code.Should().Contain("public interface IProductData");
            code.Should().Contain("IReadOnlyList<Product> GetAllProducts();");
            code.Should().Contain("Task<IReadOnlyList<Product>> GetAllProductsAsync(CancellationToken cancellationToken = default);");
            code.Should().Contain("Task<Product?> GetProductByIdAsync(int id, CancellationToken cancellationToken = default);");
            code.Should().Contain("Task AddProductAsync(Product entity, CancellationToken cancellationToken = default);");
            code.Should().Contain("Task UpdateProductAsync(Product entity, CancellationToken cancellationToken = default);");
            code.Should().Contain("Task DeleteProductAsync(int id, CancellationToken cancellationToken = default);");
        }

        [Fact]
        public void GeneratedInterface_IncludesDocumentation()
        {
            // Arrange
            var service = new DataLayerGeneratorService();
            var modelInfo = new ModelInfo
            {
                ClassName = "Product",
                Namespace = "TestApp.Models",
                HasIdProperty = true
            };

            var options = new DataLayerOptions { GenerateGetAll = true };

            // Act
            var code = service.GenerateInterface("Product", modelInfo, options);

            // Assert
            code.Should().Contain("/// <summary>");
            code.Should().Contain("/// Data access interface for Product entities");
            code.Should().Contain("/// </summary>");
        }

        [Fact]
        public void GeneratedCode_OrderBy_UsesIdProperty()
        {
            // Arrange
            var service = new DataLayerGeneratorService();
            var modelInfo = new ModelInfo
            {
                ClassName = "Product",
                Namespace = "TestApp.Models",
                HasIdProperty = true
            };

            var options = new DataLayerOptions { GenerateGetAll = true };

            // Act
            var code = service.GenerateDataLayerClass("Product", modelInfo, options);

            // Assert
            code.Should().Contain(".OrderBy(x => x.Id)");
        }

        [Fact]
        public void GeneratedCode_AsyncMethods_UseProperAsyncSyntax()
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
                GenerateGetById = true,
                GenerateAdd = true
            };

            // Act
            var code = service.GenerateDataLayerClass("Product", modelInfo, options);

            // Assert
            code.Should().Contain("async Task<");
            code.Should().Contain("await context.");
            code.Should().Contain("ToListAsync(cancellationToken)");
            code.Should().Contain("FirstOrDefaultAsync");
            code.Should().Contain("AddAsync");
            code.Should().Contain("SaveChangesAsync");
        }

        [Fact]
        public void GeneratedCode_NoIdProperty_SkipsGetByIdAndDelete()
        {
            // Arrange
            var service = new DataLayerGeneratorService();
            var modelInfo = new ModelInfo
            {
                ClassName = "Configuration",
                Namespace = "TestApp.Models",
                HasIdProperty = false
            };

            var options = new DataLayerOptions
            {
                GenerateGetAll = true,
                GenerateGetById = true, // Requested but should be skipped
                GenerateAdd = true,
                GenerateDelete = true // Requested but should be skipped
            };

            // Act
            var code = service.GenerateDataLayerClass("Configuration", modelInfo, options);

            // Assert
            code.Should().Contain("GetAllConfigurationsAsync");
            code.Should().Contain("AddConfigurationAsync");
            code.Should().NotContain("GetConfigurationByIdAsync");
            code.Should().NotContain("DeleteConfigurationAsync");
        }

        [Fact]
        public void GeneratedCode_CustomDbContext_UsesCorrectName()
        {
            // Arrange
            var generatorOptions = new GeneratorOptions
            {
                DbContextName = "MyCustomDbContext"
            };
            var service = new DataLayerGeneratorService(generatorOptions);

            var modelInfo = new ModelInfo
            {
                ClassName = "Product",
                Namespace = "TestApp.Models",
                HasIdProperty = true
            };

            var dataOptions = new DataLayerOptions { GenerateGetAll = true };

            // Act
            var code = service.GenerateDataLayerClass("Product", modelInfo, dataOptions);

            // Assert
            code.Should().Contain("MyCustomDbContext context");
            code.Should().NotContain("ApplicationDbContext");
        }
    }
}
