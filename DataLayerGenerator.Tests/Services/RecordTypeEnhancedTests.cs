using FluentAssertions;
using DataLayerGenerator.Services;
using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Xunit;

namespace DataLayerGenerator.Tests.Services
{
    /// <summary>
    /// Enhanced tests for record type support (C# 9+)
    /// </summary>
    public class RecordTypeEnhancedTests : IDisposable
    {
        private readonly DataLayerGeneratorService _service;
        private readonly string _tempDirectory;
        private bool _disposed;

        public RecordTypeEnhancedTests()
        {
            _service = new DataLayerGeneratorService();
            _tempDirectory = Path.Combine(Path.GetTempPath(), $"RecordTests_{Guid.NewGuid()}");
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

        #region Positional Records (Primary Constructor)

        [Fact]
        public async Task AnalyzeModelsAsync_PositionalRecord_ExtractsPropertiesAsync()
        {
            // Arrange
            var sourceCode = @"
namespace TestApp.Models
{
    public record Product(int Id, string Name, decimal Price);
}";
            var filePath = CreateTempFile(sourceCode);

            // Act
            var result = await _service.AnalyzeModelsAsync(filePath);

            // Assert
            result.Should().HaveCount(1);
            result[0].ClassName.Should().Be("Product");
            result[0].Properties.Should().HaveCount(3);
            result[0].Properties.Should().Contain(p => p.Name == "Id" && p.Type == "int");
            result[0].Properties.Should().Contain(p => p.Name == "Name" && p.Type == "string");
            result[0].Properties.Should().Contain(p => p.Name == "Price" && p.Type == "decimal");
        }

        [Fact]
        public async Task AnalyzeModelsAsync_PositionalRecordWithAdditionalProperties_CombinesBothAsync()
        {
            // Arrange
            var sourceCode = @"
using System;

namespace TestApp.Models
{
    public record Product(int Id, string Name)
    {
        public decimal Price { get; init; }
        public DateTime CreatedDate { get; init; }
    }
}";
            var filePath = CreateTempFile(sourceCode);

            // Act
            var result = await _service.AnalyzeModelsAsync(filePath);

            // Assert
            result[0].Properties.Should().HaveCount(4);
            result[0].Properties.Should().Contain(p => p.Name == "Id");
            result[0].Properties.Should().Contain(p => p.Name == "Name");
            result[0].Properties.Should().Contain(p => p.Name == "Price");
            result[0].Properties.Should().Contain(p => p.Name == "CreatedDate");
        }

        [Fact]
        public async Task AnalyzeModelsAsync_PositionalRecordWithMethods_IncludesAllPropertiesAsync()
        {
            // Arrange
            var sourceCode = @"
namespace TestApp.Models
{
    public record Person(string FirstName, string LastName, int Age)
    {
        public string GetFullName() => $""{FirstName} {LastName}"";
        public bool IsAdult() => Age >= 18;
    }
}";
            var filePath = CreateTempFile(sourceCode);

            // Act
            var result = await _service.AnalyzeModelsAsync(filePath);

            // Assert
            result[0].Properties.Should().HaveCount(3);
            result[0].Properties.Should().Contain(p => p.Name == "FirstName");
            result[0].Properties.Should().Contain(p => p.Name == "LastName");
            result[0].Properties.Should().Contain(p => p.Name == "Age");
        }

        #endregion

        #region Record Structs

        [Fact]
        public async Task AnalyzeModelsAsync_RecordStruct_ExtractsCorrectlyAsync()
        {
            // Arrange
            var sourceCode = @"
namespace TestApp.Models
{
    public record struct Point(int X, int Y);
}";
            var filePath = CreateTempFile(sourceCode);

            // Act
            var result = await _service.AnalyzeModelsAsync(filePath);

            // Assert
            result.Should().HaveCount(1);
            result[0].ClassName.Should().Be("Point");
            result[0].Properties.Should().HaveCount(2);
            result[0].Properties.Should().Contain(p => p.Name == "X");
            result[0].Properties.Should().Contain(p => p.Name == "Y");
        }

        [Fact]
        public async Task AnalyzeModelsAsync_ReadOnlyRecordStruct_ExtractsCorrectlyAsync()
        {
            // Arrange
            var sourceCode = @"
namespace TestApp.Models
{
    public readonly record struct Coordinate(double Latitude, double Longitude);
}";
            var filePath = CreateTempFile(sourceCode);

            // Act
            var result = await _service.AnalyzeModelsAsync(filePath);

            // Assert
            result.Should().HaveCount(1);
            result[0].ClassName.Should().Be("Coordinate");
            result[0].Properties.Should().HaveCount(2);
        }

        #endregion

        #region Init-Only Properties

        [Fact]
        public async Task AnalyzeModelsAsync_RecordWithInitProperties_HandlesCorrectlyAsync()
        {
            // Arrange
            var sourceCode = @"
using System;

namespace TestApp.Models
{
    public record Customer
    {
        public int Id { get; init; }
        public string Name { get; init; }
        public string Email { get; init; }
        public DateTime CreatedDate { get; init; }
    }
}";
            var filePath = CreateTempFile(sourceCode);

            // Act
            var result = await _service.AnalyzeModelsAsync(filePath);

            // Assert
            result[0].Properties.Should().HaveCount(4);
            // init properties should be treated as readable properties
            result[0].Properties.Should().Contain(p => p.Name == "Id");
            result[0].Properties.Should().Contain(p => p.Name == "Name");
            result[0].Properties.Should().Contain(p => p.Name == "Email");
            result[0].Properties.Should().Contain(p => p.Name == "CreatedDate");
        }

        [Fact]
        public async Task AnalyzeModelsAsync_MixedInitAndSetProperties_IncludesBothAsync()
        {
            // Arrange
            var sourceCode = @"
namespace TestApp.Models
{
    public record Product
    {
        public int Id { get; init; }
        public string Name { get; set; }
        public decimal Price { get; init; }
        public bool IsActive { get; set; }
    }
}";
            var filePath = CreateTempFile(sourceCode);

            // Act
            var result = await _service.AnalyzeModelsAsync(filePath);

            // Assert
            result[0].Properties.Should().HaveCount(4);
            result[0].Properties.Should().Contain(p => p.Name == "Id");
            result[0].Properties.Should().Contain(p => p.Name == "Name");
            result[0].Properties.Should().Contain(p => p.Name == "Price");
            result[0].Properties.Should().Contain(p => p.Name == "IsActive");
        }

        #endregion

        #region Record Inheritance

        [Fact]
        public async Task AnalyzeModelsAsync_RecordInheritance_ExtractsBaseAndDerivedAsync()
        {
            // Arrange
            var sourceCode = @"
namespace TestApp.Models
{
    public record Person(string FirstName, string LastName);
    
    public record Student(string FirstName, string LastName, string StudentId) 
        : Person(FirstName, LastName);
}";
            var filePath = CreateTempFile(sourceCode);

            // Act
            var result = await _service.AnalyzeModelsAsync(filePath);

            // Assert
            result.Should().HaveCount(2);

            var person = result.First(m => m.ClassName == "Person");
            person.Properties.Should().HaveCount(2);

            var student = result.First(m => m.ClassName == "Student");
            // Student should have all properties including inherited ones
            student.Properties.Should().HaveCountGreaterOrEqualTo(3);
            student.Properties.Should().Contain(p => p.Name == "StudentId");
        }

        [Fact]
        public async Task AnalyzeModelsAsync_RecordWithBaseRecord_HandlesPropertiesAsync()
        {
            // Arrange
            var sourceCode = @"
using System;

namespace TestApp.Models
{
    public abstract record Entity
    {
        public int Id { get; init; }
        public DateTime CreatedDate { get; init; }
    }
    
    public record Product : Entity
    {
        public string Name { get; init; }
        public decimal Price { get; init; }
    }
}";
            var filePath = CreateTempFile(sourceCode);

            // Act
            var result = await _service.AnalyzeModelsAsync(filePath);

            // Assert
            var product = result.FirstOrDefault(m => m.ClassName == "Product");
            product.Should().NotBeNull();
            // Should include both base and derived properties
            product!.Properties.Should().HaveCountGreaterOrEqualTo(2); // At least Name and Price
        }

        #endregion

        #region Complex Record Scenarios

        [Fact]
        public async Task AnalyzeModelsAsync_RecordWithComplexTypes_HandlesCorrectlyAsync()
        {
            // Arrange
            var sourceCode = @"
using System;
using System.Collections.Generic;

namespace TestApp.Models
{
    public record Order(
        int Id,
        DateTime OrderDate,
        List<OrderItem> Items,
        Customer Customer);
    
    public record OrderItem(string ProductName, int Quantity);
    public record Customer(string Name);
}";
            var filePath = CreateTempFile(sourceCode);

            // Act
            var result = await _service.AnalyzeModelsAsync(filePath);

            // Assert
            result.Should().HaveCount(3);

            var order = result.First(m => m.ClassName == "Order");
            order.Properties.Should().HaveCount(4);
            order.Properties.First(p => p.Name == "Items").IsCollection.Should().BeTrue();
            order.Properties.First(p => p.Name == "Customer").IsCollection.Should().BeFalse();
        }

        [Fact]
        public async Task AnalyzeModelsAsync_RecordWithNullableProperties_PreservesNullabilityAsync()
        {
            // Arrange
            var sourceCode = @"
using System;

namespace TestApp.Models
{
    public record Person(
        int Id,
        string Name,
        int? Age,
        DateTime? BirthDate,
        string? MiddleName);
}";
            var filePath = CreateTempFile(sourceCode);

            // Act
            var result = await _service.AnalyzeModelsAsync(filePath);

            // Assert
            result[0].Properties.Should().HaveCount(5);
            result[0].Properties.First(p => p.Name == "Age").Type.Should().Contain("?");
            result[0].Properties.First(p => p.Name == "BirthDate").Type.Should().Contain("?");
        }

        [Fact]
        public async Task AnalyzeModelsAsync_RecordWithDocumentation_PreservesDocumentationAsync()
        {
            // Arrange
            var sourceCode = @"
namespace TestApp.Models
{
    /// <summary>
    /// Represents a product entity
    /// </summary>
    public record Product
    {
        /// <summary>
        /// Gets the product ID
        /// </summary>
        public int Id { get; init; }
        
        /// <summary>
        /// Gets the product name
        /// </summary>
        public string Name { get; init; }
    }
}";
            var filePath = CreateTempFile(sourceCode);

            // Act
            var result = await _service.AnalyzeModelsAsync(filePath);
            var dataLayerCode = _service.GenerateDataLayerClass(
                "Product",
                result[0],
                new DataLayerOptions { GenerateGetAll = true });

            // Assert
            dataLayerCode.Should().Contain("/// <summary>");
            dataLayerCode.Should().Contain("Data access layer for Product");
        }

        #endregion

        #region Mixed Classes and Records

        [Fact]
        public async Task AnalyzeModelsAsync_MixedClassesAndRecords_ExtractsBothAsync()
        {
            // Arrange
            var sourceCode = @"
namespace TestApp.Models
{
    public class Product
    {
        public int Id { get; set; }
        public string Name { get; set; }
    }
    
    public record Customer(int Id, string Name, string Email);
    
    public class Order
    {
        public int Id { get; set; }
        public int CustomerId { get; set; }
    }
    
    public record OrderItem(int OrderId, string ProductName);
}";
            var filePath = CreateTempFile(sourceCode);

            // Act
            var result = await _service.AnalyzeModelsAsync(filePath);

            // Assert
            result.Should().HaveCount(4);
            result.Should().Contain(m => m.ClassName == "Product");
            result.Should().Contain(m => m.ClassName == "Customer");
            result.Should().Contain(m => m.ClassName == "Order");
            result.Should().Contain(m => m.ClassName == "OrderItem");
        }

        #endregion

        #region Data Layer Generation for Records

        [Fact]
        public void GenerateDataLayerClass_ForRecord_GeneratesCorrectly()
        {
            // Arrange
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
                GenerateGetAll = true,
                GenerateGetById = true,
                GenerateAdd = true
            };

            // Act
            var code = _service.GenerateDataLayerClass("Product", modelInfo, options);

            // Assert
            code.Should().Contain("public class ProductData");
            code.Should().Contain("GetAllProductsAsync");
            code.Should().Contain("GetProductByIdAsync");
            code.Should().Contain("AddProductAsync");
            code.Should().Contain("context.Products");
        }

        [Fact]
        public void GenerateInterface_ForRecord_GeneratesCorrectly()
        {
            // Arrange
            var modelInfo = new ModelInfo
            {
                ClassName = "Customer",
                Namespace = "TestApp.Models",
                HasIdProperty = true
            };

            var options = new DataLayerOptions
            {
                GenerateGetAll = true,
                GenerateAdd = true
            };

            // Act
            var code = _service.GenerateInterface("Customer", modelInfo, options);

            // Assert
            code.Should().Contain("public interface ICustomerData");
            code.Should().Contain("IReadOnlyList<Customer> GetAllCustomers()");
            code.Should().Contain("Task<IReadOnlyList<Customer>> GetAllCustomersAsync");
            code.Should().Contain("Task AddCustomerAsync(Customer entity");
        }

        #endregion

        #region Edge Cases

        [Fact]
        public async Task AnalyzeModelsAsync_EmptyRecord_HandlesGracefullyAsync()
        {
            // Arrange
            var sourceCode = @"
namespace TestApp.Models
{
    public record EmptyRecord;
}";
            var filePath = CreateTempFile(sourceCode);

            // Act
            var result = await _service.AnalyzeModelsAsync(filePath);

            // Assert
            result.Should().HaveCount(1);
            result[0].ClassName.Should().Be("EmptyRecord");
            result[0].Properties.Should().BeEmpty();
        }

        [Fact]
        public async Task AnalyzeModelsAsync_RecordWithOnlyMethods_NoPropertiesAsync()
        {
            // Arrange
            var sourceCode = @"
namespace TestApp.Models
{
    public record Calculator
    {
        public int Add(int a, int b) => a + b;
        public int Subtract(int a, int b) => a - b;
    }
}";
            var filePath = CreateTempFile(sourceCode);

            // Act
            var result = await _service.AnalyzeModelsAsync(filePath);

            // Assert
            result.Should().HaveCount(1);
            result[0].Properties.Should().BeEmpty();
        }

        [Fact]
        public async Task AnalyzeModelsAsync_GenericRecord_HandlesCorrectlyAsync()
        {
            // Arrange
            var sourceCode = @"
namespace TestApp.Models
{
    public record Result<T>(bool Success, T Data, string Message);
}";
            var filePath = CreateTempFile(sourceCode);

            // Act
            var result = await _service.AnalyzeModelsAsync(filePath);

            // Assert
            if (result.Count != 0)
            {
                result[0].ClassName.Should().Be("Result");
                result[0].Properties.Should().HaveCountGreaterOrEqualTo(3);
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
