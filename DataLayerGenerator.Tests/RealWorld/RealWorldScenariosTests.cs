using FluentAssertions;
using DataLayerGenerator.Services;
using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Xunit;

namespace DataLayerGenerator.Tests.RealWorld
{
    /// <summary>
    /// Tests for common real-world model patterns
    /// </summary>
    public class RealWorldScenariosTests : IDisposable
    {
        private readonly DataLayerGeneratorService _service;
        private readonly string _tempDirectory;
        private bool _disposed;

        public RealWorldScenariosTests()
        {
            _service = new DataLayerGeneratorService();
            _tempDirectory = Path.Combine(Path.GetTempPath(), $"RealWorld_{Guid.NewGuid()}");
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

        #region Audit Fields Pattern

        [Fact]
        public async Task AnalyzeModelsAsync_ModelWithAuditFields_IncludesAllPropertiesAsync()
        {
            // Arrange
            var sourceCode = @"
using System;

namespace TestApp.Models
{
    public class Product
    {
        public int Id { get; set; }
        public string Name { get; set; }
        public decimal Price { get; set; }
        
        // Audit fields
        public string CreatedBy { get; set; }
        public DateTime CreatedDate { get; set; }
        public string? UpdatedBy { get; set; }
        public DateTime? UpdatedDate { get; set; }
    }
}";
            var filePath = CreateTempFile(sourceCode);

            // Act
            var result = await _service.AnalyzeModelsAsync(filePath);

            // Assert
            result[0].Properties.Should().HaveCount(7); // Changed from 8 to 7
            result[0].Properties.Should().Contain(p => p.Name == "CreatedBy");
            result[0].Properties.Should().Contain(p => p.Name == "CreatedDate");
            result[0].Properties.Should().Contain(p => p.Name == "UpdatedBy");
            result[0].Properties.Should().Contain(p => p.Name == "UpdatedDate");
        }

        #endregion

        #region Soft Delete Pattern

        [Fact]
        public async Task AnalyzeModelsAsync_SoftDeletePattern_IncludesDeleteFieldsAsync()
        {
            // Arrange
            var sourceCode = @"
using System;

namespace TestApp.Models
{
    public class Order
    {
        public int Id { get; set; }
        public int CustomerId { get; set; }
        public decimal TotalAmount { get; set; }
        
        // Soft delete fields
        public bool IsDeleted { get; set; }
        public DateTime? DeletedDate { get; set; }
        public string? DeletedBy { get; set; }
    }
}";
            var filePath = CreateTempFile(sourceCode);

            // Act
            var result = await _service.AnalyzeModelsAsync(filePath);

            // Assert
            result[0].Properties.Should().HaveCount(6);
            result[0].Properties.Should().Contain(p => p.Name == "IsDeleted");
            result[0].Properties.Should().Contain(p => p.Name == "DeletedDate");
            result[0].Properties.Should().Contain(p => p.Name == "DeletedBy");
        }

        #endregion

        #region Multi-Tenant Pattern

        [Fact]
        public async Task AnalyzeModelsAsync_MultiTenantModel_IncludesTenantIdAsync()
        {
            // Arrange
            var sourceCode = @"
namespace TestApp.Models
{
    public class Customer
    {
        public int Id { get; set; }
        public int TenantId { get; set; }
        public string Name { get; set; }
        public string Email { get; set; }
    }
}";
            var filePath = CreateTempFile(sourceCode);

            // Act
            var result = await _service.AnalyzeModelsAsync(filePath);

            // Assert
            result[0].Properties.Should().HaveCount(4);
            result[0].Properties.Should().Contain(p => p.Name == "TenantId");
        }

        #endregion

        #region Composite Keys Pattern

        [Fact]
        public async Task AnalyzeModelsAsync_CompositeKeyModel_IncludesAllKeyPropertiesAsync()
        {
            // Arrange
            var sourceCode = @"
namespace TestApp.Models
{
    public class OrderItem
    {
        public int OrderId { get; set; }
        public int ProductId { get; set; }
        public int Quantity { get; set; }
        public decimal UnitPrice { get; set; }
    }
}";
            var filePath = CreateTempFile(sourceCode);

            // Act
            var result = await _service.AnalyzeModelsAsync(filePath);

            // Assert
            result[0].Properties.Should().HaveCount(4);
            result[0].Properties.Should().Contain(p => p.Name == "OrderId");
            result[0].Properties.Should().Contain(p => p.Name == "ProductId");
            result[0].HasIdProperty.Should().BeFalse(); // No single "Id" property
        }

        #endregion

        #region Data Annotations Pattern

        [Fact]
        public async Task AnalyzeModelsAsync_ModelWithDataAnnotations_IncludesAllPropertiesAsync()
        {
            // Arrange
            var sourceCode = @"
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace TestApp.Models
{
    [Table(""Products"")]
    public class Product
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int Id { get; set; }
        
        [Required]
        [MaxLength(100)]
        public string Name { get; set; }
        
        [Column(TypeName = ""decimal(18,2)"")]
        [Range(0, 999999)]
        public decimal Price { get; set; }
    }
}";
            var filePath = CreateTempFile(sourceCode);

            // Act
            var result = await _service.AnalyzeModelsAsync(filePath);

            // Assert
            result[0].Properties.Should().HaveCount(3);
            result[0].Properties.Should().Contain(p => p.Name == "Id");
            result[0].Properties.Should().Contain(p => p.Name == "Name");
            result[0].Properties.Should().Contain(p => p.Name == "Price");
        }

        #endregion

        #region Navigation Properties

        [Fact]
        public async Task AnalyzeModelsAsync_OneToManyRelationship_DetectsCorrectlyAsync()
        {
            // Arrange
            var sourceCode = @"
using System.Collections.Generic;

namespace TestApp.Models
{
    public class Order
    {
        public int Id { get; set; }
        public int CustomerId { get; set; }
        
        // Navigation properties
        public Customer Customer { get; set; }
        public ICollection<OrderItem> OrderItems { get; set; }
    }

    public class Customer
    {
        public int Id { get; set; }
        public string Name { get; set; }
    }

    public class OrderItem
    {
        public int Id { get; set; }
        public string ProductName { get; set; }
    }
}";
            var filePath = CreateTempFile(sourceCode);

            // Act
            var result = await _service.AnalyzeModelsAsync(filePath);

            // Assert
            var order = result.FirstOrDefault(m => m.ClassName == "Order");
            order.Should().NotBeNull();

            order!.Properties.Should().HaveCount(4);

            var orderItemsProperty = order.Properties.FirstOrDefault(p => p.Name == "OrderItems");
            orderItemsProperty.Should().NotBeNull();
            orderItemsProperty!.IsCollection.Should().BeTrue();

            var customerProperty = order.Properties.FirstOrDefault(p => p.Name == "Customer");
            customerProperty.Should().NotBeNull();
            customerProperty!.IsCollection.Should().BeFalse();
        }

        [Fact]
        public async Task AnalyzeModelsAsync_ManyToManyRelationship_DetectsCorrectlyAsync()
        {
            // Arrange
            var sourceCode = @"
using System.Collections.Generic;

namespace TestApp.Models
{
    public class Student
    {
        public int Id { get; set; }
        public string Name { get; set; }
        public ICollection<StudentCourse> StudentCourses { get; set; }
    }

    public class Course
    {
        public int Id { get; set; }
        public string Title { get; set; }
        public ICollection<StudentCourse> StudentCourses { get; set; }
    }

    public class StudentCourse
    {
        public int StudentId { get; set; }
        public int CourseId { get; set; }
        public Student Student { get; set; }
        public Course Course { get; set; }
    }
}";
            var filePath = CreateTempFile(sourceCode);

            // Act
            var result = await _service.AnalyzeModelsAsync(filePath);

            // Assert
            result.Should().HaveCount(3);
            var student = result.First(m => m.ClassName == "Student");
            student.Properties.First(p => p.Name == "StudentCourses").IsCollection.Should().BeTrue();
        }

        #endregion

        #region Inheritance Pattern

        [Fact]
        public async Task AnalyzeModelsAsync_TablePerHierarchy_HandlesBaseClassAsync()
        {
            // Arrange
            var sourceCode = @"
using System;

namespace TestApp.Models
{
    public abstract class Entity
    {
        public int Id { get; set; }
        public DateTime CreatedDate { get; set; }
    }

    public class Product : Entity
    {
        public string Name { get; set; }
        public decimal Price { get; set; }
    }
}";
            var filePath = CreateTempFile(sourceCode);

            // Act
            var result = await _service.AnalyzeModelsAsync(filePath);

            // Assert
            var product = result.FirstOrDefault(m => m.ClassName == "Product");
            product.Should().NotBeNull();
            // Should have at least its own properties
            product?.Properties.Should().HaveCountGreaterOrEqualTo(2);
        }

        #endregion

        #region Value Objects

        [Fact]
        public async Task AnalyzeModelsAsync_ValueObject_ExtractsCorrectlyAsync()
        {
            // Arrange
            var sourceCode = @"
namespace TestApp.Models
{
    public class Address
    {
        public string Street { get; set; }
        public string City { get; set; }
        public string State { get; set; }
        public string ZipCode { get; set; }
        public string Country { get; set; }
    }
}";
            var filePath = CreateTempFile(sourceCode);

            // Act
            var result = await _service.AnalyzeModelsAsync(filePath);

            // Assert
            result[0].ClassName.Should().Be("Address");
            result[0].Properties.Should().HaveCount(5);
            result[0].HasIdProperty.Should().BeFalse(); // Value objects typically don't have Id
        }

        #endregion

        #region Enums

        [Fact]
        public async Task AnalyzeModelsAsync_ModelWithEnumProperties_IncludesEnumsAsync()
        {
            // Arrange
            var sourceCode = @"
using System;

namespace TestApp.Models
{
    public class Order
    {
        public int Id { get; set; }
        public OrderStatus Status { get; set; }
        public PaymentMethod PaymentMethod { get; set; }
        public DateTime OrderDate { get; set; }
    }

    public enum OrderStatus
    {
        Pending,
        Processing,
        Shipped,
        Delivered,
        Cancelled
    }

    public enum PaymentMethod
    {
        CreditCard,
        PayPal,
        BankTransfer
    }
}";
            var filePath = CreateTempFile(sourceCode);

            // Act
            var result = await _service.AnalyzeModelsAsync(filePath);

            // Assert
            var order = result.First(m => m.ClassName == "Order");
            order.Properties.Should().HaveCount(4);
            order.Properties.Should().Contain(p => p.Name == "Status");
            order.Properties.Should().Contain(p => p.Name == "PaymentMethod");
        }

        #endregion

        #region Complex Types

        [Fact]
        public async Task AnalyzeModelsAsync_NestedComplexTypes_HandlesCorrectlyAsync()
        {
            // Arrange
            var sourceCode = @"
using System;
using System.Collections.Generic;

namespace TestApp.Models
{
    public class BlogPost
    {
        public int Id { get; set; }
        public string Title { get; set; }
        public string Content { get; set; }
        public Author Author { get; set; }
        public List<Comment> Comments { get; set; }
        public List<string> Tags { get; set; }
        public PostMetadata Metadata { get; set; }
    }

    public class Author
    {
        public string Name { get; set; }
        public string Email { get; set; }
    }

    public class Comment
    {
        public string Text { get; set; }
        public DateTime CreatedDate { get; set; }
    }

    public class PostMetadata
    {
        public int ViewCount { get; set; }
        public int LikeCount { get; set; }
    }
}";
            var filePath = CreateTempFile(sourceCode);

            // Act
            var result = await _service.AnalyzeModelsAsync(filePath);

            // Assert
            var blogPost = result.First(m => m.ClassName == "BlogPost");
            blogPost.Properties.Should().HaveCount(7);
            blogPost.Properties.First(p => p.Name == "Comments").IsCollection.Should().BeTrue();
            blogPost.Properties.First(p => p.Name == "Tags").IsCollection.Should().BeTrue();
            blogPost.Properties.First(p => p.Name == "Author").IsCollection.Should().BeFalse();
        }

        #endregion

        #region Read-Only Models

        [Fact]
        public async Task AnalyzeModelsAsync_ReadOnlyModel_IncludesAllPropertiesAsync()
        {
            // Arrange
            var sourceCode = @"
using System;

namespace TestApp.Models
{
    public class ReportData
    {
        public int Id { get; }
        public string Name { get; }
        public decimal TotalSales { get; }
        public DateTime ReportDate { get; }

        public ReportData(int id, string name, decimal totalSales, DateTime reportDate)
        {
            Id = id;
            Name = name;
            TotalSales = totalSales;
            ReportDate = reportDate;
        }
    }
}";
            var filePath = CreateTempFile(sourceCode);

            // Act
            var result = await _service.AnalyzeModelsAsync(filePath);

            // Assert
            result[0].Properties.Should().HaveCount(4);
            result[0].Properties.Should().Contain(p => p.Name == "Id");
            result[0].Properties.Should().Contain(p => p.Name == "Name");
        }

        #endregion

        #region Timestamp/RowVersion

        [Fact]
        public async Task AnalyzeModelsAsync_WithConcurrencyToken_IncludesTimestampAsync()
        {
            // Arrange
            var sourceCode = @"
using System;
using System.ComponentModel.DataAnnotations;

namespace TestApp.Models
{
    public class Product
    {
        public int Id { get; set; }
        public string Name { get; set; }
        public decimal Price { get; set; }
        
        [Timestamp]
        public byte[] RowVersion { get; set; }
    }
}";
            var filePath = CreateTempFile(sourceCode);

            // Act
            var result = await _service.AnalyzeModelsAsync(filePath);

            // Assert
            result[0].Properties.Should().HaveCount(4);
            result[0].Properties.Should().Contain(p => p.Name == "RowVersion");
        }

        #endregion

        #region JSON Columns

        [Fact]
        public async Task AnalyzeModelsAsync_JsonColumn_IncludesPropertyAsync()
        {
            // Arrange
            var sourceCode = @"
using System.Collections.Generic;

namespace TestApp.Models
{
    public class Product
    {
        public int Id { get; set; }
        public string Name { get; set; }
        
        // JSON column - EF Core 7+
        public Dictionary<string, string> Attributes { get; set; }
        public ProductSettings Settings { get; set; }
    }

    public class ProductSettings
    {
        public bool IsVisible { get; set; }
        public int StockLevel { get; set; }
    }
}";
            var filePath = CreateTempFile(sourceCode);

            // Act
            var result = await _service.AnalyzeModelsAsync(filePath);

            // Assert
            var product = result.First(m => m.ClassName == "Product");
            product.Properties.Should().HaveCount(4);
            product.Properties.Should().Contain(p => p.Name == "Attributes");
            product.Properties.Should().Contain(p => p.Name == "Settings");
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
