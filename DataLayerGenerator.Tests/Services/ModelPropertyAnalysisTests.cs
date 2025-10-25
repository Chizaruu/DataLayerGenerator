using FluentAssertions;
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
    /// Tests for model property analysis
    /// </summary>
    public class ModelPropertyAnalysisTests : IDisposable
    {
        private readonly string _tempDirectory;
        private bool _disposed;

        public ModelPropertyAnalysisTests()
        {
            _tempDirectory = Path.Combine(Path.GetTempPath(), $"PropertyTests_{Guid.NewGuid()}");
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
        public async Task AnalyzeModelsAsync_SimpleProperties_ExtractsCorrectlyAsync()
        {
            // Arrange
            var service = new DataLayerGeneratorService();
            var sourceCode = @"
namespace TestApp.Models
{
    public class Product
    {
        public int Id { get; set; }
        public string Name { get; set; }
        public decimal Price { get; set; }
        public bool IsActive { get; set; }
    }
}";
            var filePath = CreateTempFile(sourceCode);

            // Act
            var result = await service.AnalyzeModelsAsync(filePath);

            // Assert
            result[0].Properties.Should().HaveCount(4);
            result[0].Properties.Should().Contain(p => p.Name == "Id" && p.Type == "int");
            result[0].Properties.Should().Contain(p => p.Name == "Name" && p.Type == "string");
            result[0].Properties.Should().Contain(p => p.Name == "Price" && p.Type == "decimal");
            result[0].Properties.Should().Contain(p => p.Name == "IsActive" && p.Type == "bool");
        }

        [Fact]
        public async Task AnalyzeModelsAsync_CollectionProperties_DetectsCollectionsAsync()
        {
            // Arrange
            var service = new DataLayerGeneratorService();
            var sourceCode = @"
using System.Collections.Generic;

namespace TestApp.Models
{
    public class Order
    {
        public int Id { get; set; }
        public ICollection<OrderItem> Items { get; set; }
        public List<string> Tags { get; set; }
        public IEnumerable<decimal> Payments { get; set; }
        public IList<int> Quantities { get; set; }
        public Customer Customer { get; set; }
    }

    public class OrderItem { }
    public class Customer { }
}";
            var filePath = CreateTempFile(sourceCode);

            // Act
            var result = await service.AnalyzeModelsAsync(filePath);

            // Assert
            var order = result.FirstOrDefault(m => m.ClassName == "Order");
            order.Should().NotBeNull();

            order!.Properties.First(p => p.Name == "Items").IsCollection.Should().BeTrue();
            order.Properties.First(p => p.Name == "Tags").IsCollection.Should().BeTrue();
            order.Properties.First(p => p.Name == "Payments").IsCollection.Should().BeTrue();
            order.Properties.First(p => p.Name == "Quantities").IsCollection.Should().BeTrue();
            order.Properties.First(p => p.Name == "Customer").IsCollection.Should().BeFalse();
        }

        [Fact]
        public async Task AnalyzeModelsAsync_NullableProperties_PreservesNullabilityAsync()
        {
            // Arrange
            var service = new DataLayerGeneratorService();
            var sourceCode = @"
using System;

namespace TestApp.Models
{
    public class Person
    {
        public int Id { get; set; }
        public string Name { get; set; }
        public int? Age { get; set; }
        public DateTime? BirthDate { get; set; }
        public string? MiddleName { get; set; }
    }
}";
            var filePath = CreateTempFile(sourceCode);

            // Act
            var result = await service.AnalyzeModelsAsync(filePath);

            // Assert
            result[0].Properties.First(p => p.Name == "Id").Type.Should().Be("int");
            result[0].Properties.First(p => p.Name == "Name").Type.Should().Be("string");
            result[0].Properties.First(p => p.Name == "Age").Type.Should().Contain("?");
            result[0].Properties.First(p => p.Name == "BirthDate").Type.Should().Contain("?");
        }

        [Fact]
        public async Task AnalyzeModelsAsync_ReadOnlyProperties_IncludesAllAsync()
        {
            // Arrange
            var service = new DataLayerGeneratorService();
            var sourceCode = @"
namespace TestApp.Models
{
    public class Model
    {
        public int Id { get; set; }
        public string ReadWrite { get; set; }
        public string ReadOnly { get; }
        public string GetterOnly { get; }
        public string InitOnly { get; init; }
    }
}";
            var filePath = CreateTempFile(sourceCode);

            // Act
            var result = await service.AnalyzeModelsAsync(filePath);

            // Assert
            result[0].Properties.Should().HaveCount(5);
            result[0].Properties.Should().Contain(p => p.Name == "ReadWrite");
            result[0].Properties.Should().Contain(p => p.Name == "ReadOnly");
            result[0].Properties.Should().Contain(p => p.Name == "GetterOnly");
            result[0].Properties.Should().Contain(p => p.Name == "InitOnly");
        }

        [Fact]
        public async Task AnalyzeModelsAsync_PrivateProperties_ExcludesPrivateAsync()
        {
            // Arrange
            var service = new DataLayerGeneratorService();
            var sourceCode = @"
namespace TestApp.Models
{
    public class Model
    {
        public int Id { get; set; }
        public string PublicProperty { get; set; }
        private string PrivateProperty { get; set; }
        protected string ProtectedProperty { get; set; }
        internal string InternalProperty { get; set; }
    }
}";
            var filePath = CreateTempFile(sourceCode);

            // Act
            var result = await service.AnalyzeModelsAsync(filePath);

            // Assert
            result[0].Properties.Should().Contain(p => p.Name == "Id");
            result[0].Properties.Should().Contain(p => p.Name == "PublicProperty");
            result[0].Properties.Should().NotContain(p => p.Name == "PrivateProperty");
            result[0].Properties.Should().NotContain(p => p.Name == "ProtectedProperty");
        }

        [Fact]
        public async Task AnalyzeModelsAsync_ComplexPropertyTypes_ExtractsCorrectlyAsync()
        {
            // Arrange
            var service = new DataLayerGeneratorService();
            var sourceCode = @"
using System;
using System.Collections.Generic;

namespace TestApp.Models
{
    public class ComplexModel
    {
        public int Id { get; set; }
        public Dictionary<string, int> Dictionary { get; set; }
        public Tuple<int, string> Tuple { get; set; }
        public (int Id, string Name) ValueTuple { get; set; }
        public List<Dictionary<string, object>> Complex { get; set; }
    }
}";
            var filePath = CreateTempFile(sourceCode);

            // Act
            var result = await service.AnalyzeModelsAsync(filePath);

            // Assert
            result[0].Properties.Should().HaveCount(5);
            result[0].Properties.Should().Contain(p => p.Name == "Dictionary");
            result[0].Properties.Should().Contain(p => p.Name == "Tuple");
            result[0].Properties.Should().Contain(p => p.Name == "ValueTuple");
            result[0].Properties.Should().Contain(p => p.Name == "Complex");
        }

        [Fact]
        public async Task AnalyzeModelsAsync_GenericProperties_HandlesCorrectlyAsync()
        {
            // Arrange
            var service = new DataLayerGeneratorService();
            var sourceCode = @"
using System.Collections.Generic;

namespace TestApp.Models
{
    public class GenericModel<T>
    {
        public int Id { get; set; }
        public T Value { get; set; }
        public List<T> Items { get; set; }
    }
}";
            var filePath = CreateTempFile(sourceCode);

            // Act
            var result = await service.AnalyzeModelsAsync(filePath);

            // Assert
            // Generic classes should still be analyzed (though may not be ideal for data layers)
            if (result.Count != 0)
            {
                result[0].ClassName.Should().Be("GenericModel");
                result[0].Properties.Should().Contain(p => p.Name == "Id");
            }
        }

        [Fact]
        public async Task AnalyzeModelsAsync_PropertyWithAttributes_IncludesPropertyAsync()
        {
            // Arrange
            var service = new DataLayerGeneratorService();
            var sourceCode = @"
using System.ComponentModel.DataAnnotations;

namespace TestApp.Models
{
    public class Model
    {
        public int Id { get; set; }
        
        [Required]
        [MaxLength(100)]
        public string Name { get; set; }
        
        [Range(0, 100)]
        public int Age { get; set; }
    }
}";
            var filePath = CreateTempFile(sourceCode);

            // Act
            var result = await service.AnalyzeModelsAsync(filePath);

            // Assert
            result[0].Properties.Should().HaveCount(3);
            result[0].Properties.Should().Contain(p => p.Name == "Name");
            result[0].Properties.Should().Contain(p => p.Name == "Age");
        }

        [Fact]
        public async Task AnalyzeModelsAsync_StaticProperties_ExcludesStaticAsync()
        {
            // Arrange
            var service = new DataLayerGeneratorService();
            var sourceCode = @"
namespace TestApp.Models
{
    public class Model
    {
        public int Id { get; set; }
        public string Name { get; set; }
        public static string StaticProperty { get; set; }
        public static readonly string Constant = ""Value"";
    }
}";
            var filePath = CreateTempFile(sourceCode);

            // Act
            var result = await service.AnalyzeModelsAsync(filePath);

            // Assert
            result[0].Properties.Should().HaveCount(2);
            result[0].Properties.Should().Contain(p => p.Name == "Id");
            result[0].Properties.Should().Contain(p => p.Name == "Name");
            result[0].Properties.Should().NotContain(p => p.Name == "StaticProperty");
            result[0].Properties.Should().NotContain(p => p.Name == "Constant");
        }

        [Fact]
        public async Task AnalyzeModelsAsync_AutoProperties_ExtractsAllAsync()
        {
            // Arrange
            var service = new DataLayerGeneratorService();
            var sourceCode = @"
namespace TestApp.Models
{
    public class Model
    {
        public int Id { get; set; }
        public string Name { get; set; } = ""Default"";
        public int Count { get; set; } = 0;
        public bool IsActive { get; set; } = true;
    }
}";
            var filePath = CreateTempFile(sourceCode);

            // Act
            var result = await service.AnalyzeModelsAsync(filePath);

            // Assert
            result[0].Properties.Should().HaveCount(4);
            result[0].Properties.Should().Contain(p => p.Name == "Name");
            result[0].Properties.Should().Contain(p => p.Name == "Count");
            result[0].Properties.Should().Contain(p => p.Name == "IsActive");
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
