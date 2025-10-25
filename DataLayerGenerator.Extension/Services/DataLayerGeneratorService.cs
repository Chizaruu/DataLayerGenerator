using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using DataLayerGenerator.Options;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DataLayerGenerator.Services
{
    public class DataLayerGeneratorService
    {
        private readonly GeneratorOptions _options;

        public DataLayerGeneratorService(GeneratorOptions options = null)
        {
            _options = options ?? new GeneratorOptions();
        }

        public async Task<List<ModelInfo>> AnalyzeModelsAsync(string filePath)
        {
            if (filePath == null)
                throw new ArgumentNullException(nameof(filePath));

            // Then check for empty/whitespace to throw ArgumentException
            if (string.IsNullOrWhiteSpace(filePath))
                throw new ArgumentException("File path cannot be empty or whitespace.", nameof(filePath));

            if (!File.Exists(filePath))
                throw new FileNotFoundException("The specified file was not found.", filePath);

            return await Task.Run(() => AnalyzeModels(filePath));
        }

        private List<ModelInfo> AnalyzeModels(string filePath)
        {
            try
            {
                var sourceCode = File.ReadAllText(filePath);
                var tree = CSharpSyntaxTree.ParseText(sourceCode);
                var root = tree.GetRoot();

                // Find all public classes AND records that could be models
                // Use TypeDeclarationSyntax to catch both classes and records
                var typeDeclarations = root.DescendantNodes()
                    .OfType<TypeDeclarationSyntax>()
                    .Where(t => t.Modifiers.Any(m => m.IsKind(SyntaxKind.PublicKeyword)))
                    .Where(t => t is ClassDeclarationSyntax || t is RecordDeclarationSyntax)
                    .ToList();

                if (!typeDeclarations.Any())
                {
                    return new List<ModelInfo>();
                }

                var results = new List<ModelInfo>();

                // Extract using statements
                var usings = root.DescendantNodes()
                    .OfType<UsingDirectiveSyntax>()
                    .Select(u => u.ToString())
                    .Distinct()
                    .ToList();

                foreach (var typeDecl in typeDeclarations)
                {
                    var className = typeDecl.Identifier.Text;

                    // Extract namespace
                    var namespaceDeclaration = typeDecl.Ancestors()
                        .OfType<BaseNamespaceDeclarationSyntax>()
                        .FirstOrDefault();

                    var namespaceName = namespaceDeclaration?.Name.ToString() ?? "DefaultNamespace";

                    // Extract properties (handles both regular properties and record parameters)
                    var properties = ExtractProperties(typeDecl);

                    // Detect if this is likely an EF model (has properties, possibly has Key attribute)
                    bool hasIdProperty = properties.Any(p =>
                        p.Name.Equals("Id", StringComparison.OrdinalIgnoreCase) ||
                        p.Name.Equals($"{className}Id", StringComparison.OrdinalIgnoreCase));

                    results.Add(new ModelInfo
                    {
                        ClassName = className,
                        Namespace = namespaceName,
                        Properties = properties,
                        Usings = usings,
                        FilePath = filePath,
                        HasIdProperty = hasIdProperty
                    });
                }

                return results;
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException($"Failed to analyze model: {ex.Message}", ex);
            }
        }

        private List<PropertyInfo> ExtractProperties(TypeDeclarationSyntax typeDecl)
        {
            var properties = new List<PropertyInfo>();

            // Extract regular properties (excluding static)
            var propertyDeclarations = typeDecl.Members
                .OfType<PropertyDeclarationSyntax>()
                .Where(p => p.Modifiers.Any(m => m.IsKind(SyntaxKind.PublicKeyword)))
                .Where(p => !p.Modifiers.Any(m => m.IsKind(SyntaxKind.StaticKeyword))); // Exclude static properties

            foreach (var prop in propertyDeclarations)
            {
                properties.Add(new PropertyInfo
                {
                    Name = prop.Identifier.Text,
                    Type = prop.Type.ToString(),
                    IsCollection = IsCollectionType(prop.Type.ToString())
                });
            }

            // For records with primary constructors, extract parameters as properties
            if (typeDecl is RecordDeclarationSyntax recordDecl && recordDecl.ParameterList != null)
            {
                properties.AddRange(from parameter in recordDecl.ParameterList.Parameters// Avoid duplicates if the record also has explicit property declarations
                                    where !properties.Any(p => p.Name.Equals(parameter.Identifier.Text, StringComparison.OrdinalIgnoreCase))
                                    select new PropertyInfo
                                    {
                                        Name = parameter.Identifier.Text,
                                        Type = parameter.Type?.ToString() ?? "object",
                                        IsCollection = parameter.Type != null && IsCollectionType(parameter.Type.ToString())
                                    });
            }

            return properties;
        }

        private static bool IsCollectionType(string typeName)
        {
            return typeName.Contains("ICollection") ||
                   typeName.Contains("IEnumerable") ||
                   typeName.Contains("List<") ||
                   typeName.Contains("IList");
        }

        public string GenerateDataLayerClass(string className, ModelInfo modelInfo, DataLayerOptions options)
        {
            // Add parameter validation
            if (string.IsNullOrWhiteSpace(className))
                throw new ArgumentException("Class name cannot be null or empty.", nameof(className));

            if (modelInfo == null)
                throw new ArgumentNullException(nameof(modelInfo));

            if (options == null)
                throw new ArgumentNullException(nameof(options));

            var sb = new StringBuilder();

            var dataClassName = $"{className}{_options.DataLayerSuffix}";
            var interfaceName = $"I{dataClassName}";
            var dbContextName = _options.DbContextName;
            var targetNamespace = modelInfo.Namespace.EndsWith(".Models")
                ? modelInfo.Namespace.Replace(".Models", _options.DataLayerNamespaceSuffix)
                : $"{modelInfo.Namespace}{_options.DataLayerNamespaceSuffix}";

            // Add using statements
            sb.AppendLine("using Microsoft.EntityFrameworkCore;");
            sb.AppendLine($"using {modelInfo.Namespace};");

            if (options.GenerateInterface && _options.CreateInterfacesFolder)
            {
                sb.AppendLine($"using {targetNamespace}.Interfaces;");
            }

            sb.AppendLine();
            sb.AppendLine($"namespace {targetNamespace};");
            sb.AppendLine();

            // Add XML documentation only if enabled
            if (_options.AddXmlDocumentation)
            {
                sb.AppendLine($"/// <summary>");
                sb.AppendLine($"/// Data access layer for {className} entities");
                sb.AppendLine($"/// </summary>");
            }

            // Class declaration with primary constructor
            if (options.GenerateInterface)
            {
                sb.AppendLine($"public class {dataClassName}({dbContextName} context) : {interfaceName}");
            }
            else
            {
                sb.AppendLine($"public class {dataClassName}({dbContextName} context)");
            }
            sb.AppendLine("{");

            // Generate CRUD methods based on options
            if (options.GenerateGetAll)
            {
                GenerateGetAllMethods(sb, className);
            }

            if (options.GenerateGetById && modelInfo.HasIdProperty)
            {
                GenerateGetByIdMethod(sb, className);
            }

            if (options.GenerateAdd)
            {
                GenerateAddMethod(sb, className);
            }

            if (options.GenerateUpdate)
            {
                GenerateUpdateMethod(sb, className);
            }

            if (options.GenerateDelete && modelInfo.HasIdProperty)
            {
                GenerateDeleteMethod(sb, className);
            }

            if (options.GenerateCustomQueries)
            {
                GenerateCustomQueryPlaceholder(sb, className);
            }

            sb.AppendLine("}");

            return sb.ToString();
        }

        private void GenerateGetAllMethods(StringBuilder sb, string className)
        {
            // Synchronous method
            if (_options.AddXmlDocumentation)
            {
                sb.AppendLine($"    /// <summary>");
                sb.AppendLine($"    /// Gets all {className} entities");
                sb.AppendLine($"    /// </summary>");
            }
            sb.AppendLine($"    public IReadOnlyList<{className}> GetAll{className}s()");
            sb.AppendLine($"    {{");
            sb.AppendLine($"        return [.. context.{className}s"); // Changed from {contextVar} to context
            sb.AppendLine($"            .AsNoTracking()");
            sb.AppendLine($"            .OrderBy(x => x.Id)];");
            sb.AppendLine($"    }}");
            sb.AppendLine();

            // Asynchronous method
            if (_options.AddXmlDocumentation)
            {
                sb.AppendLine($"    /// <summary>");
                sb.AppendLine($"    /// Gets all {className} entities asynchronously");
                sb.AppendLine($"    /// </summary>");
            }
            sb.AppendLine($"    public async Task<IReadOnlyList<{className}>> GetAll{className}sAsync(CancellationToken cancellationToken = default)");
            sb.AppendLine($"    {{");
            sb.AppendLine($"        return await context.{className}s"); // Changed from {contextVar}
            sb.AppendLine($"            .AsNoTracking()");
            sb.AppendLine($"            .OrderBy(x => x.Id)");
            sb.AppendLine($"            .ToListAsync(cancellationToken);");
            sb.AppendLine($"    }}");
            sb.AppendLine();
        }

        private void GenerateGetByIdMethod(StringBuilder sb, string className)
        {
            if (_options.AddXmlDocumentation)
            {
                sb.AppendLine($"    /// <summary>");
                sb.AppendLine($"    /// Gets a {className} entity by ID");
                sb.AppendLine($"    /// </summary>");
            }
            sb.AppendLine($"    public async Task<{className}?> Get{className}ByIdAsync(int id, CancellationToken cancellationToken = default)");
            sb.AppendLine($"    {{");
            sb.AppendLine($"        return await context.{className}s"); // Changed from {contextVar}
            sb.AppendLine($"            .AsNoTracking()");
            sb.AppendLine($"            .FirstOrDefaultAsync(x => x.Id == id, cancellationToken);");
            sb.AppendLine($"    }}");
            sb.AppendLine();
        }

        private void GenerateAddMethod(StringBuilder sb, string className)
        {
            if (_options.AddXmlDocumentation)
            {
                sb.AppendLine($"    /// <summary>");
                sb.AppendLine($"    /// Adds a new {className} entity");
                sb.AppendLine($"    /// </summary>");
            }
            sb.AppendLine($"    public async Task Add{className}Async({className} entity, CancellationToken cancellationToken = default)");
            sb.AppendLine($"    {{");
            sb.AppendLine($"        await context.{className}s.AddAsync(entity, cancellationToken);"); // Changed
            sb.AppendLine($"        await context.SaveChangesAsync(cancellationToken);"); // Changed
            sb.AppendLine($"    }}");
            sb.AppendLine();
        }

        private void GenerateUpdateMethod(StringBuilder sb, string className)
        {
            if (_options.AddXmlDocumentation)
            {
                sb.AppendLine($"    /// <summary>");
                sb.AppendLine($"    /// Updates an existing {className} entity");
                sb.AppendLine($"    /// </summary>");
            }
            sb.AppendLine($"    public async Task Update{className}Async({className} entity, CancellationToken cancellationToken = default)");
            sb.AppendLine($"    {{");
            sb.AppendLine($"        context.{className}s.Update(entity);"); // Changed
            sb.AppendLine($"        await context.SaveChangesAsync(cancellationToken);"); // Changed
            sb.AppendLine($"    }}");
            sb.AppendLine();
        }

        private void GenerateDeleteMethod(StringBuilder sb, string className)
        {
            if (_options.AddXmlDocumentation)
            {
                sb.AppendLine($"    /// <summary>");
                sb.AppendLine($"    /// Deletes a {className} entity by ID");
                sb.AppendLine($"    /// </summary>");
            }
            sb.AppendLine($"    public async Task Delete{className}Async(int id, CancellationToken cancellationToken = default)");
            sb.AppendLine($"    {{");
            sb.AppendLine($"        var entity = await context.{className}s.FindAsync([id], cancellationToken);"); // Changed
            sb.AppendLine($"        if (entity != null)");
            sb.AppendLine($"        {{");
            sb.AppendLine($"            context.{className}s.Remove(entity);"); // Changed
            sb.AppendLine($"            await context.SaveChangesAsync(cancellationToken);"); // Changed
            sb.AppendLine($"        }}");
            sb.AppendLine($"    }}");
            sb.AppendLine();
        }

        private static void GenerateCustomQueryPlaceholder(StringBuilder sb, string className)
        {
            sb.AppendLine($"    // TODO: Add custom query methods for {className} here");
            sb.AppendLine($"    // Example:");
            sb.AppendLine($"    // public async Task<IReadOnlyList<{className}>> GetActive{className}sAsync(CancellationToken cancellationToken = default)");
            sb.AppendLine($"    // {{");
            sb.AppendLine($"    //     return await context.{className}s");
            sb.AppendLine($"    //         .AsNoTracking()");
            sb.AppendLine($"    //         .Where(x => x.IsActive)");
            sb.AppendLine($"    //         .ToListAsync(cancellationToken);");
            sb.AppendLine($"    // }}");
            sb.AppendLine();
        }

        public string GenerateInterface(string className, ModelInfo modelInfo, DataLayerOptions options)
        {
            // Add parameter validation
            if (string.IsNullOrWhiteSpace(className))
                throw new ArgumentException("Class name cannot be null or empty.", nameof(className));

            if (modelInfo == null)
                throw new ArgumentNullException(nameof(modelInfo));

            if (options == null)
                throw new ArgumentNullException(nameof(options));

            var sb = new StringBuilder();

            var dataClassName = $"{className}{_options.DataLayerSuffix}";
            var interfaceName = $"I{dataClassName}";
            var targetNamespace = modelInfo.Namespace.EndsWith(".Models")
                ? modelInfo.Namespace.Replace(".Models", $"{_options.DataLayerNamespaceSuffix}.Interfaces")
                : $"{modelInfo.Namespace}{_options.DataLayerNamespaceSuffix}.Interfaces";

            // Add using statements
            sb.AppendLine($"using {modelInfo.Namespace};");
            sb.AppendLine();
            sb.AppendLine($"namespace {targetNamespace};");
            sb.AppendLine();

            // Interface declaration
            if (_options.AddXmlDocumentation)
            {
                sb.AppendLine($"/// <summary>");
                sb.AppendLine($"/// Data access interface for {className} entities");
                sb.AppendLine($"/// </summary>");
            }
            sb.AppendLine($"public interface {interfaceName}");
            sb.AppendLine("{");

            // Method signatures
            if (options.GenerateGetAll)
            {
                sb.AppendLine($"    IReadOnlyList<{className}> GetAll{className}s();");
                sb.AppendLine($"    Task<IReadOnlyList<{className}>> GetAll{className}sAsync(CancellationToken cancellationToken = default);");
            }

            if (options.GenerateGetById && modelInfo.HasIdProperty)
            {
                sb.AppendLine($"    Task<{className}?> Get{className}ByIdAsync(int id, CancellationToken cancellationToken = default);");
            }

            if (options.GenerateAdd)
            {
                sb.AppendLine($"    Task Add{className}Async({className} entity, CancellationToken cancellationToken = default);");
            }

            if (options.GenerateUpdate)
            {
                sb.AppendLine($"    Task Update{className}Async({className} entity, CancellationToken cancellationToken = default);");
            }

            if (options.GenerateDelete && modelInfo.HasIdProperty)
            {
                sb.AppendLine($"    Task Delete{className}Async(int id, CancellationToken cancellationToken = default);");
            }

            sb.AppendLine("}");

            return sb.ToString();
        }
    }

    public class ModelInfo
    {
        public string ClassName { get; set; }
        public string Namespace { get; set; }
        public List<PropertyInfo> Properties { get; set; }
        public List<string> Usings { get; set; }
        public string FilePath { get; set; }
        public bool HasIdProperty { get; set; }
    }

    public class PropertyInfo
    {
        public string Name { get; set; }
        public string Type { get; set; }
        public bool IsCollection { get; set; }
    }

    public class DataLayerOptions
    {
        public bool GenerateInterface { get; set; } = true;
        public bool GenerateGetAll { get; set; } = true;
        public bool GenerateGetById { get; set; } = true;
        public bool GenerateAdd { get; set; } = true;
        public bool GenerateUpdate { get; set; } = true;
        public bool GenerateDelete { get; set; } = true;
        public bool GenerateCustomQueries { get; set; } = false;
    }
}