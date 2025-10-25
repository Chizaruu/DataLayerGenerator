using System.IO;
using System.Text;

namespace DataLayerGenerator.Tests.Helpers
{
    /// <summary>
    /// Helper methods for creating test scenarios
    /// </summary>
    public static class TestHelpers
    {
        /// <summary>
        /// Creates a temporary C# file with the given source code
        /// </summary>
        public static string CreateTempCSharpFile(string sourceCode, string directory)
        {
            var fileName = $"Test_{System.Guid.NewGuid()}.cs";
            var filePath = Path.Combine(directory, fileName);
            File.WriteAllText(filePath, sourceCode);
            return filePath;
        }

        /// <summary>
        /// Sample model source code for testing
        /// </summary>
        public static class SampleModels
        {
            public static string SimpleModel => @"
using System;

namespace TestApp.Models
{
    public class Product
    {
        public int Id { get; set; }
        public string Name { get; set; }
        public decimal Price { get; set; }
    }
}";

            public static string ModelWithDocumentation => @"
using System;

namespace TestApp.Models
{
    /// <summary>
    /// Represents a customer entity
    /// </summary>
    public class Customer
    {
        /// <summary>
        /// Gets or sets the customer ID
        /// </summary>
        public int Id { get; set; }

        /// <summary>
        /// Gets or sets the customer name
        /// </summary>
        public string Name { get; set; }

        public string Email { get; set; }
    }
}";

            public static string ModelWithNavigationProperties => @"
using System;
using System.Collections.Generic;

namespace TestApp.Models
{
    public class Order
    {
        public int Id { get; set; }
        public int CustomerId { get; set; }
        public DateTime OrderDate { get; set; }
        public decimal TotalAmount { get; set; }
        
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
        public int OrderId { get; set; }
        public string ProductName { get; set; }
    }
}";

            public static string MultipleModels => @"
using System;

namespace TestApp.Models
{
    public class Product
    {
        public int Id { get; set; }
        public string Name { get; set; }
    }

    public class Category
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

            public static string ModelWithoutId => @"
namespace TestApp.Models
{
    public class Configuration
    {
        public string Key { get; set; }
        public string Value { get; set; }
        public bool IsActive { get; set; }
    }
}";

            public static string ModelWithDifferentIdName => @"
namespace TestApp.Models
{
    public class Employee
    {
        public int EmployeeId { get; set; }
        public string FirstName { get; set; }
        public string LastName { get; set; }
    }
}";

            public static string EmptyModel => @"
namespace TestApp.Models
{
    public class EmptyModel
    {
    }
}";

            public static string ModelWithOnlyPrivateProperties => @"
namespace TestApp.Models
{
    public class PrivateOnlyModel
    {
        private int id;
        private string name;
        private string Name { get; set; }
    }
}";

            public static string InternalModel => @"
namespace TestApp.Models
{
    internal class InternalDataModel
    {
        public int Id { get; set; }
        public string Data { get; set; }
    }
}";

            public static string ModelWithComplexTypes => @"
using System;
using System.Collections.Generic;

namespace TestApp.Models
{
    public class BlogPost
    {
        public int Id { get; set; }
        public string Title { get; set; }
        public string Content { get; set; }
        public DateTime PublishedDate { get; set; }
        public PostStatus Status { get; set; }
        public List<string> Tags { get; set; }
        public Author Author { get; set; }
    }

    public enum PostStatus
    {
        Draft,
        Published,
        Archived
    }

    public class Author
    {
        public int Id { get; set; }
        public string Name { get; set; }
    }
}";

            public static string ModelWithNullableProperties => @"
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

            public static string RecordModel => @"
namespace TestApp.Models
{
    public record Product(int Id, string Name, decimal Price);
}";

            public static string RecordWithProperties => @"
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
        }

        /// <summary>
        /// Expected data layer code outputs for validation
        /// </summary>
        public static class ExpectedDataLayers
        {
            public static string SimpleDataLayer => @"using Microsoft.EntityFrameworkCore;
using TestApp.Models;
using TestApp.Data.Interfaces;

namespace TestApp.Data;

/// <summary>
/// Data access layer for Product entities
/// </summary>
public class ProductData(ApplicationDbContext context) : IProductData
{
    /// <summary>
    /// Gets all Product entities
    /// </summary>
    public IReadOnlyList<Product> GetAllProducts()
    {
        return [.. context.Products
            .AsNoTracking()
            .OrderBy(x => x.Id)];
    }

    /// <summary>
    /// Gets all Product entities asynchronously
    /// </summary>
    public async Task<IReadOnlyList<Product>> GetAllProductsAsync(CancellationToken cancellationToken = default)
    {
        return await context.Products
            .AsNoTracking()
            .OrderBy(x => x.Id)
            .ToListAsync(cancellationToken);
    }

    /// <summary>
    /// Gets a Product entity by ID
    /// </summary>
    public async Task<Product?> GetProductByIdAsync(int id, CancellationToken cancellationToken = default)
    {
        return await context.Products
            .AsNoTracking()
            .FirstOrDefaultAsync(x => x.Id == id, cancellationToken);
    }

    /// <summary>
    /// Adds a new Product entity
    /// </summary>
    public async Task AddProductAsync(Product entity, CancellationToken cancellationToken = default)
    {
        await context.Products.AddAsync(entity, cancellationToken);
        await context.SaveChangesAsync(cancellationToken);
    }

    /// <summary>
    /// Updates an existing Product entity
    /// </summary>
    public async Task UpdateProductAsync(Product entity, CancellationToken cancellationToken = default)
    {
        context.Products.Update(entity);
        await context.SaveChangesAsync(cancellationToken);
    }

    /// <summary>
    /// Deletes a Product entity by ID
    /// </summary>
    public async Task DeleteProductAsync(int id, CancellationToken cancellationToken = default)
    {
        var entity = await context.Products.FindAsync([id], cancellationToken);
        if (entity != null)
        {
            context.Products.Remove(entity);
            await context.SaveChangesAsync(cancellationToken);
        }
    }
}";

            public static string SimpleInterface => @"using TestApp.Models;

namespace TestApp.Data.Interfaces;

/// <summary>
/// Data access interface for Product entities
/// </summary>
public interface IProductData
{
    IReadOnlyList<Product> GetAllProducts();
    Task<IReadOnlyList<Product>> GetAllProductsAsync(CancellationToken cancellationToken = default);
    Task<Product?> GetProductByIdAsync(int id, CancellationToken cancellationToken = default);
    Task AddProductAsync(Product entity, CancellationToken cancellationToken = default);
    Task UpdateProductAsync(Product entity, CancellationToken cancellationToken = default);
    Task DeleteProductAsync(int id, CancellationToken cancellationToken = default);
}";
        }

        /// <summary>
        /// Normalizes whitespace in strings for comparison
        /// </summary>
        public static string NormalizeWhitespace(string input)
        {
            if (string.IsNullOrEmpty(input))
                return input;

            var lines = input.Split(['\r', '\n'], System.StringSplitOptions.RemoveEmptyEntries);
            var normalized = new StringBuilder();

            foreach (var line in lines)
            {
                var trimmed = line.Trim();
                if (!string.IsNullOrEmpty(trimmed))
                {
                    normalized.AppendLine(trimmed);
                }
            }

            return normalized.ToString().Trim();
        }

        /// <summary>
        /// Compares two code strings ignoring whitespace differences
        /// </summary>
        public static bool CodeEquals(string expected, string actual)
        {
            return NormalizeWhitespace(expected) == NormalizeWhitespace(actual);
        }

        /// <summary>
        /// Checks if code contains expected key elements
        /// </summary>
        public static bool ContainsKeyElements(string code, params string[] elements)
        {
            var normalizedCode = NormalizeWhitespace(code);
            return elements.All(element => normalizedCode.Contains(NormalizeWhitespace(element)));
        }
    }
}
