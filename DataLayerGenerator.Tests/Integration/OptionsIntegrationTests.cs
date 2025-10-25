using FluentAssertions;
using DataLayerGenerator.Options;
using DataLayerGenerator.Services;
using System;
using System.IO;
using System.Threading.Tasks;
using Xunit;

namespace DataLayerGenerator.Tests.Integration
{
    /// <summary>
    /// Integration tests for options and their effect on generated code
    /// </summary>
    public class OptionsIntegrationTests : IDisposable
    {
        private readonly string _tempDirectory;
        private bool _disposed;

        public OptionsIntegrationTests()
        {
            _tempDirectory = Path.Combine(Path.GetTempPath(), $"OptionsIntTests_{Guid.NewGuid()}");
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

        #region DbContext Name Option

        [Theory]
        [InlineData("ApplicationDbContext")]
        [InlineData("MyDbContext")]
        [InlineData("DataContext")]
        [InlineData("EFContext")]
        public async Task DbContextName_Option_AffectsGeneratedCodeAsync(string contextName)
        {
            // Arrange
            var options = new GeneratorOptions { DbContextName = contextName };
            var service = new DataLayerGeneratorService(options);

            var sourceCode = @"
namespace TestApp.Models
{
    public class Product
    {
        public int Id { get; set; }
        public string Name { get; set; }
    }
}";
            var filePath = CreateTempFile(sourceCode);
            var models = await service.AnalyzeModelsAsync(filePath);

            // Act
            var dataLayerCode = service.GenerateDataLayerClass(
                "Product",
                models[0],
                new DataLayerOptions { GenerateGetAll = true });

            // Assert
            dataLayerCode.Should().Contain($"{contextName} context");
            dataLayerCode.Should().Contain($"public class ProductData({contextName} context)");
        }

        #endregion

        #region Folder Name Option

        [Theory]
        [InlineData("Data", ".Data")]
        [InlineData("Repositories", ".Repositories")]
        [InlineData("DataAccess", ".DataAccess")]
        [InlineData("DAL", ".DAL")]
        public async Task FolderName_Option_AffectsNamespaceAsync(string folderName, string namespaceSuffix)
        {
            // Arrange
            var options = new GeneratorOptions
            {
                DataLayerFolderName = folderName,
                DataLayerNamespaceSuffix = namespaceSuffix
            };
            var service = new DataLayerGeneratorService(options);

            var sourceCode = @"
namespace TestApp.Models
{
    public class Product
    {
        public int Id { get; set; }
    }
}";
            var filePath = CreateTempFile(sourceCode);
            var models = await service.AnalyzeModelsAsync(filePath);

            // Act
            var dataLayerCode = service.GenerateDataLayerClass(
                "Product",
                models[0],
                new DataLayerOptions { GenerateGetAll = true });

            // Assert
            dataLayerCode.Should().Contain($"namespace TestApp{namespaceSuffix};");
        }

        #endregion

        #region Suffix Option

        [Theory]
        [InlineData("Data", "ProductData")]
        [InlineData("Repository", "ProductRepository")]
        [InlineData("Service", "ProductService")]
        [InlineData("Store", "ProductStore")]
        [InlineData("", "Product")]
        public async Task ClassSuffix_Option_AffectsClassNameAsync(string suffix, string expectedClassName)
        {
            // Arrange
            var options = new GeneratorOptions { DataLayerSuffix = suffix };
            var service = new DataLayerGeneratorService(options);

            var sourceCode = @"
namespace TestApp.Models
{
    public class Product
    {
        public int Id { get; set; }
    }
}";
            var filePath = CreateTempFile(sourceCode);
            var models = await service.AnalyzeModelsAsync(filePath);

            // Act
            var dataLayerCode = service.GenerateDataLayerClass(
                "Product",
                models[0],
                new DataLayerOptions { GenerateGetAll = true });

            // Assert
            dataLayerCode.Should().Contain($"public class {expectedClassName}");
        }

        [Theory]
        [InlineData("Data", "IProductData")]
        [InlineData("Repository", "IProductRepository")]
        [InlineData("Service", "IProductService")]
        public async Task ClassSuffix_Option_AffectsInterfaceNameAsync(string suffix, string expectedInterfaceName)
        {
            // Arrange
            var options = new GeneratorOptions { DataLayerSuffix = suffix };
            var service = new DataLayerGeneratorService(options);

            var sourceCode = @"
namespace TestApp.Models
{
    public class Product { public int Id { get; set; } }
}";
            var filePath = CreateTempFile(sourceCode);
            var models = await service.AnalyzeModelsAsync(filePath);

            // Act
            var interfaceCode = service.GenerateInterface(
                "Product",
                models[0],
                new DataLayerOptions { GenerateGetAll = true });

            // Assert
            interfaceCode.Should().Contain($"public interface {expectedInterfaceName}");
        }

        #endregion

        #region Interface Generation Option

        [Fact]
        public async Task GenerateInterfaces_False_NoInterfaceImplementationAsync()
        {
            // Arrange
            var options = new GeneratorOptions { GenerateInterfaces = false };
            var service = new DataLayerGeneratorService(options);

            var sourceCode = @"
namespace TestApp.Models
{
    public class Product { public int Id { get; set; } }
}";
            var filePath = CreateTempFile(sourceCode);
            var models = await service.AnalyzeModelsAsync(filePath);

            // Act
            var dataLayerCode = service.GenerateDataLayerClass(
                "Product",
                models[0],
                new DataLayerOptions
                {
                    GenerateInterface = false,
                    GenerateGetAll = true
                });

            // Assert
            dataLayerCode.Should().Contain("public class ProductData");
            dataLayerCode.Should().NotContain(": IProductData");
            dataLayerCode.Should().NotContain("IProductData");
        }

        [Fact]
        public async Task GenerateInterfaces_True_IncludesInterfaceImplementationAsync()
        {
            // Arrange
            var options = new GeneratorOptions { GenerateInterfaces = true };
            var service = new DataLayerGeneratorService(options);

            var sourceCode = @"
namespace TestApp.Models
{
    public class Product { public int Id { get; set; } }
}";
            var filePath = CreateTempFile(sourceCode);
            var models = await service.AnalyzeModelsAsync(filePath);

            // Act
            var dataLayerCode = service.GenerateDataLayerClass(
                "Product",
                models[0],
                new DataLayerOptions
                {
                    GenerateInterface = true,
                    GenerateGetAll = true
                });

            // Assert
            dataLayerCode.Should().Contain("public class ProductData");
            dataLayerCode.Should().Contain(": IProductData");
        }

        #endregion

        #region CRUD Method Options

        [Fact]
        public async Task GenerateGetAll_False_OmitsGetAllMethodsAsync()
        {
            // Arrange
            var service = new DataLayerGeneratorService();
            var sourceCode = @"
namespace TestApp.Models
{
    public class Product { public int Id { get; set; } }
}";
            var filePath = CreateTempFile(sourceCode);
            var models = await service.AnalyzeModelsAsync(filePath);

            // Act
            var dataLayerCode = service.GenerateDataLayerClass(
                "Product",
                models[0],
                new DataLayerOptions
                {
                    GenerateGetAll = false,
                    GenerateAdd = true
                });

            // Assert
            dataLayerCode.Should().NotContain("GetAllProducts");
            dataLayerCode.Should().NotContain("GetAllProductsAsync");
            dataLayerCode.Should().Contain("AddProductAsync");
        }

        [Fact]
        public async Task GenerateGetById_False_OmitsGetByIdMethodAsync()
        {
            // Arrange
            var service = new DataLayerGeneratorService();
            var sourceCode = @"
namespace TestApp.Models
{
    public class Product { public int Id { get; set; } }
}";
            var filePath = CreateTempFile(sourceCode);
            var models = await service.AnalyzeModelsAsync(filePath);

            // Act
            var dataLayerCode = service.GenerateDataLayerClass(
                "Product",
                models[0],
                new DataLayerOptions
                {
                    GenerateGetById = false,
                    GenerateGetAll = true
                });

            // Assert
            dataLayerCode.Should().NotContain("GetProductByIdAsync");
            dataLayerCode.Should().Contain("GetAllProductsAsync");
        }

        [Fact]
        public async Task AllCrudMethodsFalse_GeneratesEmptyDataLayerAsync()
        {
            // Arrange
            var service = new DataLayerGeneratorService();
            var sourceCode = @"
namespace TestApp.Models
{
    public class Product { public int Id { get; set; } }
}";
            var filePath = CreateTempFile(sourceCode);
            var models = await service.AnalyzeModelsAsync(filePath);

            // Act
            var dataLayerCode = service.GenerateDataLayerClass(
                "Product",
                models[0],
                new DataLayerOptions
                {
                    GenerateGetAll = false,
                    GenerateGetById = false,
                    GenerateAdd = false,
                    GenerateUpdate = false,
                    GenerateDelete = false
                });

            // Assert
            dataLayerCode.Should().Contain("public class ProductData");
            // Should have class structure but no methods
            dataLayerCode.Should().NotContain("GetAllProducts");
            dataLayerCode.Should().NotContain("AddProduct");
            dataLayerCode.Should().NotContain("UpdateProduct");
            dataLayerCode.Should().NotContain("DeleteProduct");
        }

        #endregion

        #region Modern C# Features Options

        [Fact]
        public async Task UsePrimaryConstructor_True_GeneratesPrimaryConstructorAsync()
        {
            // Arrange
            var options = new GeneratorOptions { UsePrimaryConstructor = true };
            var service = new DataLayerGeneratorService(options);

            var sourceCode = @"
namespace TestApp.Models
{
    public class Product { public int Id { get; set; } }
}";
            var filePath = CreateTempFile(sourceCode);
            var models = await service.AnalyzeModelsAsync(filePath);

            // Act
            var dataLayerCode = service.GenerateDataLayerClass(
                "Product",
                models[0],
                new DataLayerOptions { GenerateGetAll = true });

            // Assert
            dataLayerCode.Should().Contain("public class ProductData(ApplicationDbContext context)");
            dataLayerCode.Should().NotContain("public ProductData(ApplicationDbContext context)");
            dataLayerCode.Should().NotContain("private readonly ApplicationDbContext _context");
        }

        [Fact]
        public async Task UseAsNoTracking_True_IncludesAsNoTrackingAsync()
        {
            // Arrange
            var options = new GeneratorOptions { UseAsNoTracking = true };
            var service = new DataLayerGeneratorService(options);

            var sourceCode = @"
namespace TestApp.Models
{
    public class Product { public int Id { get; set; } }
}";
            var filePath = CreateTempFile(sourceCode);
            var models = await service.AnalyzeModelsAsync(filePath);

            // Act
            var dataLayerCode = service.GenerateDataLayerClass(
                "Product",
                models[0],
                new DataLayerOptions { GenerateGetAll = true, GenerateGetById = true });

            // Assert
            dataLayerCode.Should().Contain(".AsNoTracking()");
        }

        [Fact]
        public async Task AddXmlDocumentation_True_IncludesDocCommentsAsync()
        {
            // Arrange
            var options = new GeneratorOptions { AddXmlDocumentation = true };
            var service = new DataLayerGeneratorService(options);

            var sourceCode = @"
namespace TestApp.Models
{
    public class Product { public int Id { get; set; } }
}";
            var filePath = CreateTempFile(sourceCode);
            var models = await service.AnalyzeModelsAsync(filePath);

            // Act
            var dataLayerCode = service.GenerateDataLayerClass(
                "Product",
                models[0],
                new DataLayerOptions { GenerateGetAll = true });

            // Assert
            dataLayerCode.Should().Contain("/// <summary>");
            dataLayerCode.Should().Contain("/// Data access layer for Product entities");
        }

        [Fact]
        public async Task AddXmlDocumentation_False_OmitsDocCommentsAsync()
        {
            // Arrange
            var options = new GeneratorOptions { AddXmlDocumentation = false };
            var service = new DataLayerGeneratorService(options);

            var sourceCode = @"
namespace TestApp.Models
{
    public class Product { public int Id { get; set; } }
}";
            var filePath = CreateTempFile(sourceCode);
            var models = await service.AnalyzeModelsAsync(filePath);

            // Act
            var dataLayerCode = service.GenerateDataLayerClass(
                "Product",
                models[0],
                new DataLayerOptions { GenerateGetAll = true });

            // Assert
            dataLayerCode.Should().NotContain("/// <summary>");
        }

        #endregion

        #region Multiple Options Combined

        [Fact]
        public async Task MultipleOptions_AllCustom_GeneratesCorrectlyAsync()
        {
            // Arrange
            var options = new GeneratorOptions
            {
                DbContextName = "MyDataContext",
                DataLayerSuffix = "Repository",
                DataLayerNamespaceSuffix = ".Repositories",
                GenerateInterfaces = true,
                UsePrimaryConstructor = true,
                UseAsNoTracking = true,
                AddXmlDocumentation = true
            };
            var service = new DataLayerGeneratorService(options);

            var sourceCode = @"
namespace MyApp.Models
{
    public class Product 
    { 
        public int Id { get; set; }
        public string Name { get; set; }
    }
}";
            var filePath = CreateTempFile(sourceCode);
            var models = await service.AnalyzeModelsAsync(filePath);

            // Act
            var dataLayerCode = service.GenerateDataLayerClass(
                "Product",
                models[0],
                new DataLayerOptions
                {
                    GenerateInterface = true,
                    GenerateGetAll = true,
                    GenerateAdd = true
                });

            // Assert
            dataLayerCode.Should().Contain("namespace MyApp.Repositories;");
            dataLayerCode.Should().Contain("public class ProductRepository(MyDataContext context) : IProductRepository");
            dataLayerCode.Should().Contain(".AsNoTracking()");
            dataLayerCode.Should().Contain("/// <summary>");
        }

        [Fact]
        public async Task MinimalOptions_OnlyRequired_GeneratesSimpleCodeAsync()
        {
            // Arrange
            var options = new GeneratorOptions
            {
                GenerateInterfaces = false,
                AddXmlDocumentation = false,
                GenerateGetAll = true,
                GenerateGetById = false,
                GenerateAdd = false,
                GenerateUpdate = false,
                GenerateDelete = false
            };
            var service = new DataLayerGeneratorService(options);

            var sourceCode = @"
namespace TestApp.Models
{
    public class Product { public int Id { get; set; } }
}";
            var filePath = CreateTempFile(sourceCode);
            var models = await service.AnalyzeModelsAsync(filePath);

            // Act
            var dataLayerCode = service.GenerateDataLayerClass(
                "Product",
                models[0],
                new DataLayerOptions
                {
                    GenerateInterface = false,
                    GenerateGetAll = true,
                    GenerateGetById = false,
                    GenerateAdd = false,
                    GenerateUpdate = false,
                    GenerateDelete = false
                });

            // Assert
            dataLayerCode.Should().Contain("public class ProductData");
            dataLayerCode.Should().NotContain(": IProductData");
            dataLayerCode.Should().NotContain("/// <summary>");
            dataLayerCode.Should().Contain("GetAllProducts");
            dataLayerCode.Should().NotContain("AddProduct");
        }

        #endregion

        #region Interface Folder Option

        [Fact]
        public async Task CreateInterfacesFolder_True_UsesInterfacesNamespaceAsync()
        {
            // Arrange
            var options = new GeneratorOptions
            {
                CreateInterfacesFolder = true,
                GenerateInterfaces = true
            };
            var service = new DataLayerGeneratorService(options);

            var sourceCode = @"
namespace TestApp.Models
{
    public class Product { public int Id { get; set; } }
}";
            var filePath = CreateTempFile(sourceCode);
            var models = await service.AnalyzeModelsAsync(filePath);

            // Act
            var interfaceCode = service.GenerateInterface(
                "Product",
                models[0],
                new DataLayerOptions { GenerateGetAll = true });

            // Assert
            interfaceCode.Should().Contain("namespace TestApp.Data.Interfaces");
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
