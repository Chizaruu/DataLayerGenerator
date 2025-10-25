# Data Layer Generator

A Visual Studio 2022 extension that automatically generates Entity Framework data access layer classes from C# model classes with full CRUD operations, interfaces, and customizable options.

## Features

✨ **Right-click to generate** - Works directly from Solution Explorer context menu  
✨ **Interactive CRUD selection** - Choose which operations to generate (GetAll, GetById, Add, Update, Delete)  
✨ **Interface generation** - Automatically creates interfaces for dependency injection  
✨ **Primary constructor support** - Uses modern C# 12 syntax for cleaner code  
✨ **Smart model detection** - Analyzes classes and records with automatic ID property detection  
✨ **Batch processing** - Generate data layers for multiple model files at once  
✨ **Record support** - Works with both classes and records (including primary constructors)  
✨ **Async-first** - Generates async methods with CancellationToken support  
✨ **AsNoTracking optimization** - Uses read-only queries for better performance  
✨ **XML documentation** - Generates comprehensive XML comments for all methods  
✨ **Overwrite protection** - Prompts before replacing existing files  
✨ **Detailed logging** - Progress tracking in Output Window  
✨ **Customizable options** - Configure behavior through Tools → Options  
✨ **Navigation properties** - Detects and handles EF navigation properties  
✨ **Flexible structure** - Configurable folder structure and naming conventions

## Requirements

-   Visual Studio 2022 (Community, Professional, or Enterprise)
-   .NET Framework 4.8
-   Entity Framework Core (in your target project)

## Installation

### Option 1: Build from Source

1. Install the **Visual Studio extension development** workload via Visual Studio Installer
2. Clone or download this repository
3. Open `DataLayerGenerator.sln` in Visual Studio 2022
4. Build the solution (Ctrl+Shift+B)
5. Close all Visual Studio instances
6. Run the generated `.vsix` file from `bin/Debug/` or `bin/Release/`
7. Restart Visual Studio

### Option 2: From VSIX Package

1. Download the `.vsix` file
2. Close all Visual Studio instances
3. Double-click the `.vsix` file
4. Follow the installation wizard
5. Restart Visual Studio

## Configuration

Access settings through **Tools → Options → Data Layer Generator → General**

### General Settings

| Setting                  | Default                | Description                                |
| ------------------------ | ---------------------- | ------------------------------------------ |
| Data Layer Folder Name   | `Data`                 | Folder for generated data layer files      |
| Data Layer Suffix        | `Data`                 | Suffix for data layer class names          |
| Namespace Suffix         | `.Data`                | Appended to model namespace                |
| DbContext Name           | `ApplicationDbContext` | Name of your DbContext class               |
| Generate Interfaces      | `true`                 | Generate interfaces for data layer classes |
| Create Interfaces Folder | `true`                 | Create separate 'Interfaces' subfolder     |

### CRUD Methods

| Setting                 | Default | Description                         |
| ----------------------- | ------- | ----------------------------------- |
| Generate GetAll Method  | `true`  | Generate GetAll() and GetAllAsync() |
| Generate GetById Method | `true`  | Generate GetByIdAsync()             |
| Generate Add Method     | `true`  | Generate AddAsync()                 |
| Generate Update Method  | `true`  | Generate UpdateAsync()              |
| Generate Delete Method  | `true`  | Generate DeleteAsync()              |

### Advanced Settings

| Setting                          | Default | Description                                  |
| -------------------------------- | ------- | -------------------------------------------- |
| Use Primary Constructor          | `true`  | Use C# 12 primary constructor syntax         |
| Include Custom Query Placeholder | `false` | Add commented placeholder for custom queries |
| Use AsNoTracking                 | `true`  | Use AsNoTracking() for read-only queries     |
| Add XML Documentation            | `true`  | Add XML documentation comments to methods    |

## Usage

### Quick Start

1. Create your model classes in a `Models` folder:

```csharp
namespace MyProject.Models
{
    public class Product
    {
        public int Id { get; set; }
        public string Name { get; set; }
        public decimal Price { get; set; }
        public int CategoryId { get; set; }

        // Navigation property
        public Category Category { get; set; }
    }
}
```

2. Right-click the model file in Solution Explorer
3. Select **Generate Data Layer**
4. Select which CRUD operations to include
5. Click **Generate**

The extension creates:

-   `Data/ProductData.cs` - Data access implementation
-   `Data/Interfaces/IProductData.cs` - Interface (if enabled)

### Generated Output

**ProductData.cs:**

```csharp
using Microsoft.EntityFrameworkCore;
using MyProject.Models;
using MyProject.Data.Interfaces;

namespace MyProject.Data;

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
}
```

**IProductData.cs:**

```csharp
using MyProject.Models;

namespace MyProject.Data.Interfaces;

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
}
```

### CRUD Method Selection Dialog

When generating, you can choose which methods to include:

-   ☑️ **GetAll() / GetAllAsync()** - Retrieve all entities
-   ☑️ **GetByIdAsync()** - Retrieve entity by ID
-   ☑️ **AddAsync()** - Add new entity
-   ☑️ **UpdateAsync()** - Update existing entity
-   ☑️ **DeleteAsync()** - Delete entity by ID
-   ☐ **Include placeholder for custom queries**

Quick actions:

-   **Select All** - Enable all CRUD operations
-   **Deselect All** - Disable all operations

### View Progress

1. Open Output Window (View → Output or Ctrl+Alt+O)
2. Select **Data Layer Generator** from the dropdown
3. View detailed logs:
    - Files being processed
    - Models detected
    - Files generated
    - Any errors or warnings

Example output:

```
[14:23:45] === Data Layer Generator v1.0.0 ===
[14:23:45] Starting data layer generation...
[14:23:45] Options: Folder=Data, Suffix=Data
[14:23:45]   DbContext=ApplicationDbContext, GenerateInterfaces=True
[14:23:45] Processing 1 file(s)...
[14:23:45] Analyzing: Product.cs
[14:23:45]   Found model: Product
[14:23:45]   Created folder: Data
[14:23:45]   Generated: ProductData.cs
[14:23:46]   Generated interface: IProductData.cs
[14:23:46]   Added to project: ProductData.cs
[14:23:46]   Added to project: IProductData.cs
[14:23:46] === Generation Complete ===
[14:23:46] Data layer generation complete! Succeeded: 1 Failed: 0 Skipped: 0
```

## Examples

### Record Types

**Input:**

```csharp
namespace MyProject.Models
{
    public record Customer(int Id, string Name, string Email);
}
```

**Output:** Generates full CRUD data layer and interface for the `Customer` record.

### Models Without ID Property

If a model doesn't have an `Id` or `{ClassName}Id` property:

```csharp
public class Configuration
{
    public string Key { get; set; }
    public string Value { get; set; }
}
```

The generator will:

-   ✅ Generate GetAll methods
-   ✅ Generate Add method
-   ✅ Generate Update method
-   ❌ Skip GetById method (no ID property)
-   ❌ Skip Delete method (no ID property)

### Custom ID Names

The generator recognizes both patterns:

```csharp
public class Employee
{
    public int EmployeeId { get; set; }  // Recognized as ID
    // ...
}
```

### Models with Navigation Properties

```csharp
public class Order
{
    public int Id { get; set; }
    public int CustomerId { get; set; }
    public DateTime OrderDate { get; set; }

    // Navigation properties are detected but not included in method signatures
    public Customer Customer { get; set; }
    public ICollection<OrderItem> OrderItems { get; set; }
}
```

Generated methods use the entity type directly and let EF handle navigation properties.

### Batch Processing Multiple Files

1. Select multiple model files in Solution Explorer (Ctrl+Click)
2. Right-click → **Generate Data Layer**
3. Each file shows its own dialog for method selection
4. All data layers are generated in one operation

## Dependency Injection Setup

After generating your data layers, register them in your DI container:

```csharp
// Program.cs or Startup.cs
services.AddDbContext<ApplicationDbContext>(options =>
    options.UseSqlServer(connectionString));

// Register data layers
services.AddScoped<IProductData, ProductData>();
services.AddScoped<ICustomerData, CustomerData>();
services.AddScoped<IOrderData, OrderData>();
```

Then inject and use:

```csharp
public class ProductService
{
    private readonly IProductData _productData;

    public ProductService(IProductData productData)
    {
        _productData = productData;
    }

    public async Task<IReadOnlyList<Product>> GetAllProductsAsync()
    {
        return await _productData.GetAllProductsAsync();
    }
}
```

## Troubleshooting

### Extension doesn't appear in context menu

-   Verify you're right-clicking `.cs` files in Solution Explorer
-   Check Extensions → Manage Extensions to confirm it's installed and enabled
-   Ensure the file is part of a project (not a loose file)
-   Restart Visual Studio

### Build errors in test project

-   Change test project target framework from `net8.0-windows` to `net48`
-   Visual Studio SDK packages only support .NET Framework
-   See the updated test project file in the solution

### No models found

-   Ensure the class/record is `public`
-   Verify the file contains valid C# syntax
-   Check Output Window for detailed analysis logs
-   Model must have at least one public property

### Files not added to project

-   Check Output Window for warnings about project addition
-   Manually add files from the `Data` folder if needed
-   Ensure the project file is not read-only

### Options not taking effect

-   Changes take effect immediately for new generations
-   Previously generated files are not automatically updated
-   Regenerate files to apply new options

### DbContext mismatch

-   Update **DbContext Name** in Tools → Options to match your context class
-   Regenerate data layers after changing the DbContext name
-   Ensure your DbContext has DbSet properties matching your model names (e.g., `DbSet<Product> Products`)

## Project Structure

```
DataLayerGenerator.Extension/
├── Commands/
│   └── GenerateDataLayerCommand.cs     # Command handler and orchestration
├── Services/
│   └── DataLayerGeneratorService.cs    # Roslyn-based code generation
├── Options/
│   └── GeneralOptionsPage.cs           # Settings/configuration page
├── UI/
│   ├── GenerateDataLayerDialog.xaml    # CRUD method selection dialog
│   ├── GenerateDataLayerDialog.xaml.cs
│   ├── OverwriteDialog.xaml            # File overwrite confirmation
│   ├── OverwriteDialog.xaml.cs
│   ├── PreviewDialog.xaml              # Code preview dialog
│   ├── PreviewDialog.xaml.cs
│   └── MemberSelectionItem.cs          # View model for UI items
├── Constants.cs                         # Configuration constants
├── DataLayerGeneratorPackage.cs        # VS Package entry point
└── DataLayerGeneratorPackage.vsct      # Command definitions

DataLayerGenerator.Tests/
├── Performance/
│   └── PerformanceTests.cs             # Performance benchmarks
└── Helpers/
    └── TestHelpers.cs                  # Test utilities and samples
```

## Development

### Building the Extension

1. Open `DataLayerGenerator.sln` in Visual Studio 2022
2. Ensure all NuGet packages are restored
3. Build the solution (Ctrl+Shift+B)
4. The VSIX file will be in `bin/Debug/` or `bin/Release/`

### Debugging

1. Press **F5** to launch experimental instance
2. Open a test project with model classes
3. Test the extension
4. Set breakpoints in the extension code
5. Check Output Window → "Data Layer Generator" for logs

### Running Tests

The solution includes comprehensive tests:

```bash
# Run all tests
dotnet test

# Run specific test category
dotnet test --filter "FullyQualifiedName~Performance"
```

**Note:** Test project targets .NET Framework 4.8 (not .NET 8) because Visual Studio SDK packages require .NET Framework.

### Key Technologies

-   **Roslyn (Microsoft.CodeAnalysis.CSharp)** - C# syntax parsing and code generation
-   **Visual Studio SDK 17.x** - IDE integration and extensibility
-   **WPF** - User interface dialogs
-   **VSIX** - Extension packaging and deployment
-   **xUnit + FluentAssertions** - Unit testing

## Performance

The generator is optimized for speed:

-   Simple models: < 1 second
-   Large models (100+ properties): < 2 seconds
-   50 files batch: < 20 seconds
-   Concurrent processing supported
-   Memory efficient (< 50 MB growth over 100 iterations)

Performance tests are included in the solution to ensure regression-free development.

## Known Limitations

-   Only processes public classes and records (not internal, protected, or private)
-   Nested classes are not supported
-   Assumes Entity Framework Core conventions (DbSet properties named as plural)
-   Primary key must be named `Id` or `{ClassName}Id`
-   Generated code assumes standard EF Core patterns

## Roadmap

Future enhancements under consideration:

-   [ ] Support for composite primary keys
-   [ ] Custom query template system
-   [ ] Support for repository pattern generation
-   [ ] Unit of Work pattern support
-   [ ] Specification pattern support
-   [ ] Support for Dapper or other ORMs
-   [ ] Bulk operation methods (AddRange, DeleteRange)
-   [ ] Search/filter method generation
-   [ ] Pagination support
-   [ ] Include/ThenInclude for navigation properties
-   [ ] Integration with EF migrations

## License

MIT License

Copyright (c) 2025 Developer Chizaruu

Permission is hereby granted, free of charge, to any person obtaining a copy of this software and associated documentation files (the "Software"), to deal in the Software without restriction, including without limitation the rights to use, copy, modify, merge, publish, distribute, sublicense, and/or sell copies of the Software, and to permit persons to whom the Software is furnished to do so, subject to the following conditions:

The above copyright notice and this permission notice shall be included in all copies or substantial portions of the Software.

THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY, FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM, OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE SOFTWARE.

## Contributing

Contributions welcome! Areas for improvement:

-   Additional CRUD patterns (Repository, Unit of Work)
-   Support for other data access patterns
-   Custom method templates
-   Integration with other ORMs
-   Performance optimizations
-   Additional test coverage

## Support

For issues or questions:

1. Check the **Output Window** (View → Output → "Data Layer Generator") for detailed error messages
2. Review the **Troubleshooting** section above
3. Check **Tools → Options → Data Layer Generator** for configuration options
4. Verify Visual Studio 2022 and .NET Framework 4.8 compatibility
5. Create an issue with:
    - Visual Studio version
    - Extension version
    - Steps to reproduce
    - Output Window logs
    - Sample model code
    - Current option settings

---

**Made with ❤️ for Entity Framework developers**
