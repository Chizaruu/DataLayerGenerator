using FluentAssertions;
using DataLayerGenerator.Services;
using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Xunit;

namespace DataLayerGenerator.Tests.Performance
{
    /// <summary>
    /// Performance tests to ensure the generator runs efficiently
    /// </summary>
    public class PerformanceTests : IDisposable
    {
        private readonly DataLayerGeneratorService _service;
        private readonly string _tempDirectory;
        private bool _disposed;

        public PerformanceTests()
        {
            _service = new DataLayerGeneratorService();
            _tempDirectory = Path.Combine(Path.GetTempPath(), $"PerfTests_{Guid.NewGuid()}");
            Directory.CreateDirectory(_tempDirectory);

            // Warm up Roslyn to avoid measuring initialization in tests
            WarmUpService();
        }

        private void WarmUpService()
        {
            var warmupCode = "namespace Test { public class Warmup { public int Id { get; set; } } }";
            var warmupFile = CreateTempFile(warmupCode);
            try
            {
                _service.AnalyzeModelsAsync(warmupFile).GetAwaiter().GetResult();

                // Also warm up code generation
                var modelInfo = new ModelInfo
                {
                    ClassName = "Warmup",
                    Namespace = "Test",
                    HasIdProperty = true,
                    Properties = new System.Collections.Generic.List<PropertyInfo>
                    {
                        new PropertyInfo { Name = "Id", Type = "int" }
                    }
                };
                _service.GenerateDataLayerClass("Warmup", modelInfo, new DataLayerOptions());
                _service.GenerateInterface("Warmup", modelInfo, new DataLayerOptions());
            }
            catch { /* Ignore warm-up errors */ }
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

        #region Model Analysis Performance

        [Fact]
        public async Task AnalyzeModelsAsync_SimpleModel_CompletesQuicklyAsync()
        {
            // Arrange
            var sourceCode = @"
namespace TestApp.Models
{
    public class Product
    {
        public int Id { get; set; }
        public string Name { get; set; }
        public decimal Price { get; set; }
    }
}";
            var filePath = CreateTempFile(sourceCode);
            var stopwatch = Stopwatch.StartNew();

            // Act
            var result = await _service.AnalyzeModelsAsync(filePath);
            stopwatch.Stop();

            // Assert
            result.Should().HaveCount(1);
            stopwatch.ElapsedMilliseconds.Should().BeLessThan(1000); // Should complete in < 1 second
        }

        [Fact]
        public async Task AnalyzeModelsAsync_LargeModel_CompletesReasonablyAsync()
        {
            // Arrange - Model with 100 properties
            var properties = string.Join("\n",
                Enumerable.Range(1, 100)
                    .Select(i => $"        public string Property{i} {{ get; set; }}"));

            var sourceCode = $@"
namespace TestApp.Models
{{
    public class LargeModel
    {{
        public int Id {{ get; set; }}
{properties}
    }}
}}";
            var filePath = CreateTempFile(sourceCode);
            var stopwatch = Stopwatch.StartNew();

            // Act
            var result = await _service.AnalyzeModelsAsync(filePath);
            stopwatch.Stop();

            // Assert
            result.Should().HaveCount(1);
            result[0].Properties.Should().HaveCount(101);
            stopwatch.ElapsedMilliseconds.Should().BeLessThan(2000); // Should complete in < 2 seconds
        }

        [Fact]
        public async Task AnalyzeModelsAsync_MultipleModels_CompletesReasonablyAsync()
        {
            // Arrange - 50 models in one file
            var models = string.Join("\n\n",
                Enumerable.Range(1, 50)
                    .Select(i => $@"    public class Model{i}
    {{
        public int Id {{ get; set; }}
        public string Name{i} {{ get; set; }}
        public decimal Value{i} {{ get; set; }}
    }}"));

            var sourceCode = $@"
namespace TestApp.Models
{{
{models}
}}";
            var filePath = CreateTempFile(sourceCode);
            var stopwatch = Stopwatch.StartNew();

            // Act
            var result = await _service.AnalyzeModelsAsync(filePath);
            stopwatch.Stop();

            // Assert
            result.Should().HaveCount(50);
            stopwatch.ElapsedMilliseconds.Should().BeLessThan(3000); // Should complete in < 3 seconds
        }

        #endregion

        #region Code Generation Performance

        [Fact]
        public void GenerateDataLayerClass_SimpleModel_CompletesQuickly()
        {
            // Arrange
            var modelInfo = new ModelInfo
            {
                ClassName = "Product",
                Namespace = "TestApp.Models",
                HasIdProperty = true,
                Properties = Enumerable.Range(1, 10)
                    .Select(i => new PropertyInfo { Name = $"Property{i}", Type = "string" })
                    .ToList()
            };

            var options = new DataLayerOptions
            {
                GenerateGetAll = true,
                GenerateGetById = true,
                GenerateAdd = true,
                GenerateUpdate = true,
                GenerateDelete = true
            };

            var stopwatch = Stopwatch.StartNew();

            // Act
            var result = _service.GenerateDataLayerClass("Product", modelInfo, options);
            stopwatch.Stop();

            // Assert
            result.Should().NotBeNullOrEmpty();
            stopwatch.ElapsedMilliseconds.Should().BeLessThan(500); // Should complete in < 0.5 seconds
        }

        [Fact]
        public void GenerateDataLayerClass_LargeModel_CompletesReasonably()
        {
            // Arrange - Model with 100 properties
            var modelInfo = new ModelInfo
            {
                ClassName = "LargeModel",
                Namespace = "TestApp.Models",
                HasIdProperty = true,
                Properties = Enumerable.Range(1, 100)
                    .Select(i => new PropertyInfo { Name = $"Property{i}", Type = "string" })
                    .ToList()
            };

            var options = new DataLayerOptions
            {
                GenerateGetAll = true,
                GenerateGetById = true,
                GenerateAdd = true,
                GenerateUpdate = true,
                GenerateDelete = true
            };

            var stopwatch = Stopwatch.StartNew();

            // Act
            var result = _service.GenerateDataLayerClass("LargeModel", modelInfo, options);
            stopwatch.Stop();

            // Assert
            result.Should().NotBeNullOrEmpty();
            stopwatch.ElapsedMilliseconds.Should().BeLessThan(1000); // Should complete in < 1 second
        }

        [Fact]
        public void GenerateInterface_CompletesQuickly()
        {
            // Arrange
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

            var stopwatch = Stopwatch.StartNew();

            // Act
            var result = _service.GenerateInterface("Product", modelInfo, options);
            stopwatch.Stop();

            // Assert
            result.Should().NotBeNullOrEmpty();
            stopwatch.ElapsedMilliseconds.Should().BeLessThan(200); // Should complete in < 0.2 seconds
        }

        #endregion

        #region Batch Processing Performance

        [Fact]
        public async Task BatchProcessing_10Files_CompletesReasonablyAsync()
        {
            // Arrange - 10 model files
            var files = Enumerable.Range(1, 10)
                .Select(i => CreateTempFile($@"
namespace TestApp.Models
{{
    public class Model{i}
    {{
        public int Id {{ get; set; }}
        public string Name {{ get; set; }}
        public decimal Value {{ get; set; }}
    }}
}}"))
                .ToList();

            var stopwatch = Stopwatch.StartNew();

            // Act
            foreach (var file in files)
            {
                var models = await _service.AnalyzeModelsAsync(file);
                foreach (var model in models)
                {
                    _service.GenerateDataLayerClass(
                        model.ClassName,
                        model,
                        new DataLayerOptions { GenerateGetAll = true });
                }
            }
            stopwatch.Stop();

            // Assert
            stopwatch.ElapsedMilliseconds.Should().BeLessThan(5000); // Should complete in < 5 seconds
        }

        [Fact]
        public async Task BatchProcessing_50Files_CompletesReasonablyAsync()
        {
            // Arrange - 50 model files
            var files = Enumerable.Range(1, 50)
                .Select(i => CreateTempFile($@"
namespace TestApp.Models
{{
    public class Model{i}
    {{
        public int Id {{ get; set; }}
        public string Name {{ get; set; }}
    }}
}}"))
                .ToList();

            var stopwatch = Stopwatch.StartNew();

            // Act
            foreach (var file in files)
            {
                var models = await _service.AnalyzeModelsAsync(file);
                foreach (var model in models)
                {
                    _service.GenerateDataLayerClass(
                        model.ClassName,
                        model,
                        new DataLayerOptions { GenerateGetAll = true });
                }
            }
            stopwatch.Stop();

            // Assert
            stopwatch.ElapsedMilliseconds.Should().BeLessThan(20000); // Should complete in < 20 seconds
        }

        #endregion

        #region Memory Usage

        [Fact]
        public async Task MemoryUsage_LargeFile_StaysReasonableAsync()
        {
            // Arrange
            var initialMemory = GC.GetTotalMemory(true);

            var properties = string.Join("\n",
                Enumerable.Range(1, 1000)
                    .Select(i => $"        public string Property{i} {{ get; set; }}"));

            var sourceCode = $@"
namespace TestApp.Models
{{
    public class VeryLargeModel
    {{
{properties}
    }}
}}";
            var filePath = CreateTempFile(sourceCode);

            // Act
            var result = await _service.AnalyzeModelsAsync(filePath);
            var dataLayerCode = _service.GenerateDataLayerClass(
                "VeryLargeModel",
                result[0],
                new DataLayerOptions { GenerateGetAll = true });

            var afterMemory = GC.GetTotalMemory(false);
            var memoryUsed = (afterMemory - initialMemory) / 1024 / 1024; // Convert to MB

            // Assert
            memoryUsed.Should().BeLessThan(100); // Should use < 100 MB
        }

        [Fact]
        public async Task MemoryUsage_MultipleIterations_NoMemoryLeakAsync()
        {
            // Arrange
            var sourceCode = @"
namespace TestApp.Models
{
    public class Product { public int Id { get; set; } }
}";
            var filePath = CreateTempFile(sourceCode);

            GC.Collect();
            GC.WaitForPendingFinalizers();
            GC.Collect();
            var initialMemory = GC.GetTotalMemory(true);

            // Act - Process same file 100 times
            for (int i = 0; i < 100; i++)
            {
                var result = await _service.AnalyzeModelsAsync(filePath);
                _service.GenerateDataLayerClass(
                    "Product",
                    result[0],
                    new DataLayerOptions { GenerateGetAll = true });
            }

            GC.Collect();
            GC.WaitForPendingFinalizers();
            GC.Collect();
            var finalMemory = GC.GetTotalMemory(true);
            var memoryGrowth = (finalMemory - initialMemory) / 1024 / 1024; // MB

            // Assert - Memory shouldn't grow significantly
            memoryGrowth.Should().BeLessThan(50); // Should grow < 50 MB over 100 iterations
        }

        #endregion

        #region Concurrent Processing

        [Fact]
        public async Task ConcurrentProcessing_HandlesMultipleSimultaneousAsync()
        {
            // Arrange
            var files = Enumerable.Range(1, 10)
                .Select(i => CreateTempFile($@"
namespace TestApp.Models
{{
    public class Model{i}
    {{
        public int Id {{ get; set; }}
        public string Name {{ get; set; }}
    }}
}}"))
                .ToList();

            var stopwatch = Stopwatch.StartNew();

            // Act - Process all files concurrently
            var tasks = files.Select(async file =>
            {
                var models = await _service.AnalyzeModelsAsync(file);
                return models.Select(m => _service.GenerateDataLayerClass(
                    m.ClassName,
                    m,
                    new DataLayerOptions { GenerateGetAll = true }))
                    .ToList();
            });

            var results = await Task.WhenAll(tasks);
            stopwatch.Stop();

            // Assert
            results.Should().HaveCount(10);
            results.Should().OnlyContain(r => r.Count == 1);
            stopwatch.ElapsedMilliseconds.Should().BeLessThan(3000); // Concurrent should be faster
        }

        #endregion

        #region Comparison Benchmarks

        [Fact]
        public async Task Benchmark_SimpleModelFullGenerationAsync()
        {
            // Arrange
            var sourceCode = @"
namespace TestApp.Models
{
    public class Product
    {
        public int Id { get; set; }
        public string Name { get; set; }
        public decimal Price { get; set; }
    }
}";
            var filePath = CreateTempFile(sourceCode);

            var stopwatch = Stopwatch.StartNew();

            // Act - Full workflow
            var models = await _service.AnalyzeModelsAsync(filePath);
            var dataLayerCode = _service.GenerateDataLayerClass(
                "Product",
                models[0],
                new DataLayerOptions
                {
                    GenerateInterface = true,
                    GenerateGetAll = true,
                    GenerateGetById = true,
                    GenerateAdd = true,
                    GenerateUpdate = true,
                    GenerateDelete = true
                });
            var interfaceCode = _service.GenerateInterface(
                "Product",
                models[0],
                new DataLayerOptions { GenerateGetAll = true });

            stopwatch.Stop();

            // Assert & Log
            dataLayerCode.Should().NotBeNullOrEmpty();
            interfaceCode.Should().NotBeNullOrEmpty();

            // Log performance metrics
            Console.WriteLine($"Full generation time: {stopwatch.ElapsedMilliseconds}ms");
            stopwatch.ElapsedMilliseconds.Should().BeLessThan(1000);
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