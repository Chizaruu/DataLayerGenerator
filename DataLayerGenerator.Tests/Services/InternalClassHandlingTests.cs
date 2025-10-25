using FluentAssertions;
using DataLayerGenerator.Options;
using DataLayerGenerator.Services;
using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Xunit;

namespace DataLayerGenerator.Tests.Services
{
    /// <summary>
    /// Tests for handling internal classes and members
    /// </summary>
    public class InternalClassHandlingTests : IDisposable
    {
        private readonly string _tempDirectory;
        private bool _disposed;

        public InternalClassHandlingTests()
        {
            _tempDirectory = Path.Combine(Path.GetTempPath(), $"InternalTests_{Guid.NewGuid()}");
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
        public async Task AnalyzeModelsAsync_InternalClass_ExcludedByDefaultAsync()
        {
            // Arrange
            var service = new DataLayerGeneratorService();
            var sourceCode = @"
namespace TestApp.Models
{
    public class PublicModel
    {
        public int Id { get; set; }
        public string Name { get; set; }
    }

    internal class InternalModel
    {
        public int Id { get; set; }
        public string Name { get; set; }
    }
}";
            var filePath = CreateTempFile(sourceCode);

            // Act
            var result = await service.AnalyzeModelsAsync(filePath);

            // Assert
            result.Should().HaveCount(1);
            result[0].ClassName.Should().Be("PublicModel");
            result.Should().NotContain(m => m.ClassName == "InternalModel");
        }

        [Fact]
        public async Task AnalyzeModelsAsync_ImplicitInternalClass_ExcludedByDefaultAsync()
        {
            // Arrange
            var service = new DataLayerGeneratorService();
            var sourceCode = @"
namespace TestApp.Models
{
    public class PublicModel
    {
        public int Id { get; set; }
    }

    class ImplicitInternalModel  // No access modifier = internal
    {
        public int Id { get; set; }
    }
}";
            var filePath = CreateTempFile(sourceCode);

            // Act
            var result = await service.AnalyzeModelsAsync(filePath);

            // Assert
            result.Should().HaveCount(1);
            result[0].ClassName.Should().Be("PublicModel");
            result.Should().NotContain(m => m.ClassName == "ImplicitInternalModel");
        }

        [Fact]
        public async Task AnalyzeModelsAsync_InternalProperties_ExcludesFromDataLayerAsync()
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
        internal string InternalProperty { get; set; }
        protected string ProtectedProperty { get; set; }
        private string PrivateProperty { get; set; }
    }
}";
            var filePath = CreateTempFile(sourceCode);

            // Act
            var result = await service.AnalyzeModelsAsync(filePath);

            // Assert
            result[0].Properties.Should().HaveCount(2); // Only Id and PublicProperty
            result[0].Properties.Should().Contain(p => p.Name == "Id");
            result[0].Properties.Should().Contain(p => p.Name == "PublicProperty");
            result[0].Properties.Should().NotContain(p => p.Name == "InternalProperty");
            result[0].Properties.Should().NotContain(p => p.Name == "ProtectedProperty");
            result[0].Properties.Should().NotContain(p => p.Name == "PrivateProperty");
        }

        [Fact]
        public async Task AnalyzeModelsAsync_ProtectedInternalProperties_ExcludesFromDataLayerAsync()
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
        protected internal string ProtectedInternalProperty { get; set; }
    }
}";
            var filePath = CreateTempFile(sourceCode);

            // Act
            var result = await service.AnalyzeModelsAsync(filePath);

            // Assert
            // protected internal means protected OR internal (accessible in derived classes OR same assembly)
            // Should exclude from public interface
            result[0].Properties.Should().HaveCount(2); // Only Id and PublicProperty
            result[0].Properties.Should().NotContain(p => p.Name == "ProtectedInternalProperty");
        }

        [Fact]
        public async Task AnalyzeModelsAsync_PrivateProtectedProperties_ExcludesAsync()
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
        private protected string PrivateProtectedProperty { get; set; }
    }
}";
            var filePath = CreateTempFile(sourceCode);

            // Act
            var result = await service.AnalyzeModelsAsync(filePath);

            // Assert
            // private protected means private AND protected (accessible only in derived classes in same assembly)
            result[0].Properties.Should().HaveCount(2); // Only Id and PublicProperty
            result[0].Properties.Should().NotContain(p => p.Name == "PrivateProtectedProperty");
        }

        [Fact]
        public async Task AnalyzeModelsAsync_MixedAccessModifiers_IncludesOnlyPublicAsync()
        {
            // Arrange
            var service = new DataLayerGeneratorService();
            var sourceCode = @"
using System;

namespace TestApp.Models
{
    public class ComplexModel
    {
        public int Id { get; set; }
        public string Name { get; set; }
        internal int InternalId { get; set; }
        protected string ProtectedName { get; set; }
        private DateTime PrivateDate { get; set; }
        protected internal int ProtectedInternalValue { get; set; }
        private protected string PrivateProtectedValue { get; set; }
    }
}";
            var filePath = CreateTempFile(sourceCode);

            // Act
            var result = await service.AnalyzeModelsAsync(filePath);

            // Assert
            result[0].Properties.Should().HaveCount(2); // Only public properties
            result[0].Properties.Select(p => p.Name).Should().BeEquivalentTo(new[] { "Id", "Name" });
        }

        [Fact]
        public async Task AnalyzeModelsAsync_InternalGetterPublicSetter_HandlesCorrectlyAsync()
        {
            // Arrange
            var service = new DataLayerGeneratorService();
            var sourceCode = @"
namespace TestApp.Models
{
    public class Model
    {
        public int Id { get; set; }
        
        // Property with internal getter
        public string Name { internal get; set; }
        
        // Property with internal setter
        public int Count { get; internal set; }
    }
}";
            var filePath = CreateTempFile(sourceCode);

            // Act
            var result = await service.AnalyzeModelsAsync(filePath);

            // Assert
            // These should still be included since the property itself is public
            result[0].Properties.Should().HaveCount(3);
            result[0].Properties.Should().Contain(p => p.Name == "Id");
            result[0].Properties.Should().Contain(p => p.Name == "Name");
            result[0].Properties.Should().Contain(p => p.Name == "Count");
        }

        [Fact]
        public async Task AnalyzeModelsAsync_InternalStaticProperties_ExcludesAsync()
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
        public static string PublicStaticProperty { get; set; }
        internal static string InternalStaticProperty { get; set; }
    }
}";
            var filePath = CreateTempFile(sourceCode);

            // Act
            var result = await service.AnalyzeModelsAsync(filePath);

            // Assert
            // Static properties should be excluded regardless of access modifier
            result[0].Properties.Should().HaveCount(2);
            result[0].Properties.Should().Contain(p => p.Name == "Id");
            result[0].Properties.Should().Contain(p => p.Name == "Name");
            result[0].Properties.Should().NotContain(p => p.Name == "PublicStaticProperty");
            result[0].Properties.Should().NotContain(p => p.Name == "InternalStaticProperty");
        }

        [Fact]
        public async Task GenerateDataLayerClass_PublicModelOnly_GeneratesCorrectlyAsync()
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
        internal string InternalCode { get; set; }
    }
}";
            var filePath = CreateTempFile(sourceCode);
            var models = await service.AnalyzeModelsAsync(filePath);

            // Act
            var options = new DataLayerOptions
            {
                GenerateGetAll = true,
                GenerateAdd = true
            };
            var dataLayerCode = service.GenerateDataLayerClass("Product", models[0], options);

            // Assert
            dataLayerCode.Should().Contain("public class ProductData");
            dataLayerCode.Should().Contain("context.Products");
            // Should only handle public properties
            dataLayerCode.Should().NotContain("InternalCode");
        }

        [Fact]
        public async Task AnalyzeModelsAsync_NestedInternalClass_ExcludesAsync()
        {
            // Arrange
            var service = new DataLayerGeneratorService();
            var sourceCode = @"
namespace TestApp.Models
{
    public class OuterModel
    {
        public int Id { get; set; }
        public string Name { get; set; }
        
        internal class InternalNestedModel
        {
            public int NestedId { get; set; }
        }
    }
}";
            var filePath = CreateTempFile(sourceCode);

            // Act
            var result = await service.AnalyzeModelsAsync(filePath);

            // Assert
            result.Should().HaveCount(1);
            result[0].ClassName.Should().Be("OuterModel");
            // Nested classes typically aren't used as data models anyway
        }

        [Fact]
        public async Task AnalyzeModelsAsync_OnlyInternalClasses_ReturnsEmptyAsync()
        {
            // Arrange
            var service = new DataLayerGeneratorService();
            var sourceCode = @"
namespace TestApp.Models
{
    internal class InternalModel1
    {
        public int Id { get; set; }
    }

    internal class InternalModel2
    {
        public int Id { get; set; }
    }
}";
            var filePath = CreateTempFile(sourceCode);

            // Act
            var result = await service.AnalyzeModelsAsync(filePath);

            // Assert
            result.Should().BeEmpty();
        }

        [Fact]
        public async Task AnalyzeModelsAsync_InternalRecord_ExcludesAsync()
        {
            // Arrange
            var service = new DataLayerGeneratorService();
            var sourceCode = @"
namespace TestApp.Models
{
    public record PublicRecord(int Id, string Name);
    internal record InternalRecord(int Id, string Name);
}";
            var filePath = CreateTempFile(sourceCode);

            // Act
            var result = await service.AnalyzeModelsAsync(filePath);

            // Assert
            result.Should().HaveCount(1);
            result[0].ClassName.Should().Be("PublicRecord");
            result.Should().NotContain(m => m.ClassName == "InternalRecord");
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
