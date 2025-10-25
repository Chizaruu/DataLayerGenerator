using FluentAssertions;
using DataLayerGenerator.Options;
using Xunit;
using DataLayerGenerator.Services;

namespace DataLayerGenerator.Tests.Options
{
    /// <summary>
    /// Tests for GeneratorOptions and configuration
    /// </summary>
    public class OptionsTests
    {
        [Fact]
        public void GeneratorOptions_DefaultValues_SetCorrectly()
        {
            // Arrange & Act
            var options = new GeneratorOptions();

            // Assert
            options.DataLayerFolderName.Should().Be("Data");
            options.DataLayerSuffix.Should().Be("Data");
            options.DataLayerNamespaceSuffix.Should().Be(".Data");
            options.DbContextName.Should().Be("ApplicationDbContext");
            options.GenerateInterfaces.Should().BeTrue();
            options.CreateInterfacesFolder.Should().BeTrue();
            options.GenerateGetAll.Should().BeTrue();
            options.GenerateGetById.Should().BeTrue();
            options.GenerateAdd.Should().BeTrue();
            options.GenerateUpdate.Should().BeTrue();
            options.GenerateDelete.Should().BeTrue();
            options.UsePrimaryConstructor.Should().BeTrue();
            options.UseAsNoTracking.Should().BeTrue();
            options.AddXmlDocumentation.Should().BeTrue();
        }

        [Fact]
        public void GeneratorOptions_CustomValues_CanBeSet()
        {
            // Arrange & Act
            var options = new GeneratorOptions
            {
                DataLayerFolderName = "Repositories",
                DataLayerSuffix = "Repository",
                DataLayerNamespaceSuffix = ".Repositories",
                DbContextName = "MyDbContext",
                GenerateInterfaces = false,
                GenerateGetAll = false
            };

            // Assert
            options.DataLayerFolderName.Should().Be("Repositories");
            options.DataLayerSuffix.Should().Be("Repository");
            options.DataLayerNamespaceSuffix.Should().Be(".Repositories");
            options.DbContextName.Should().Be("MyDbContext");
            options.GenerateInterfaces.Should().BeFalse();
            options.GenerateGetAll.Should().BeFalse();
        }

        [Fact]
        public void DataLayerOptions_DefaultValues_SetCorrectly()
        {
            // Arrange & Act
            var options = new DataLayerOptions();

            // Assert
            options.GenerateInterface.Should().BeTrue();
            options.GenerateGetAll.Should().BeTrue();
            options.GenerateGetById.Should().BeTrue();
            options.GenerateAdd.Should().BeTrue();
            options.GenerateUpdate.Should().BeTrue();
            options.GenerateDelete.Should().BeTrue();
            options.GenerateCustomQueries.Should().BeFalse();
        }

        [Fact]
        public void DataLayerOptions_AllMethodsDisabled_IsValid()
        {
            // Arrange & Act
            var options = new DataLayerOptions
            {
                GenerateGetAll = false,
                GenerateGetById = false,
                GenerateAdd = false,
                GenerateUpdate = false,
                GenerateDelete = false
            };

            // Assert - This should be prevented by UI validation, but model allows it
            options.GenerateGetAll.Should().BeFalse();
            options.GenerateGetById.Should().BeFalse();
            options.GenerateAdd.Should().BeFalse();
            options.GenerateUpdate.Should().BeFalse();
            options.GenerateDelete.Should().BeFalse();
        }

        [Fact]
        public void GeneratorOptions_FolderName_CanBeEmpty()
        {
            // Arrange & Act
            var options = new GeneratorOptions
            {
                DataLayerFolderName = ""
            };

            // Assert - Validation should happen in DialogPage
            options.DataLayerFolderName.Should().BeEmpty();
        }

        [Fact]
        public void GeneratorOptions_Suffix_CanBeEmpty()
        {
            // Arrange & Act
            var options = new GeneratorOptions
            {
                DataLayerSuffix = ""
            };

            // Assert
            options.DataLayerSuffix.Should().BeEmpty();
        }

        [Theory]
        [InlineData("Data")]
        [InlineData("Repositories")]
        [InlineData("DataAccess")]
        [InlineData("DAL")]
        public void GeneratorOptions_CommonFolderNames_AreValid(string folderName)
        {
            // Arrange & Act
            var options = new GeneratorOptions
            {
                DataLayerFolderName = folderName
            };

            // Assert
            options.DataLayerFolderName.Should().Be(folderName);
        }

        [Theory]
        [InlineData("Data")]
        [InlineData("Repository")]
        [InlineData("Service")]
        [InlineData("Store")]
        public void GeneratorOptions_CommonSuffixes_AreValid(string suffix)
        {
            // Arrange & Act
            var options = new GeneratorOptions
            {
                DataLayerSuffix = suffix
            };

            // Assert
            options.DataLayerSuffix.Should().Be(suffix);
        }

        [Theory]
        [InlineData("ApplicationDbContext")]
        [InlineData("MyDbContext")]
        [InlineData("DataContext")]
        [InlineData("EFContext")]
        public void GeneratorOptions_CommonDbContextNames_AreValid(string contextName)
        {
            // Arrange & Act
            var options = new GeneratorOptions
            {
                DbContextName = contextName
            };

            // Assert
            options.DbContextName.Should().Be(contextName);
        }

        [Fact]
        public void GeneratorOptions_AllCrudMethodsEnabled_IsDefault()
        {
            // Arrange & Act
            var options = new GeneratorOptions();

            // Assert
            options.GenerateGetAll.Should().BeTrue();
            options.GenerateGetById.Should().BeTrue();
            options.GenerateAdd.Should().BeTrue();
            options.GenerateUpdate.Should().BeTrue();
            options.GenerateDelete.Should().BeTrue();
        }

        [Fact]
        public void GeneratorOptions_InterfacesEnabledByDefault()
        {
            // Arrange & Act
            var options = new GeneratorOptions();

            // Assert
            options.GenerateInterfaces.Should().BeTrue();
            options.CreateInterfacesFolder.Should().BeTrue();
        }

        [Fact]
        public void GeneratorOptions_ModernFeaturesEnabledByDefault()
        {
            // Arrange & Act
            var options = new GeneratorOptions();

            // Assert
            options.UsePrimaryConstructor.Should().BeTrue();
            options.UseAsNoTracking.Should().BeTrue();
            options.AddXmlDocumentation.Should().BeTrue();
        }

        [Fact]
        public void GeneratorOptions_CustomQueryPlaceholderDisabledByDefault()
        {
            // Arrange & Act
            var options = new GeneratorOptions();

            // Assert
            options.IncludeCustomQueryPlaceholder.Should().BeFalse();
        }

        [Fact]
        public void DataLayerOptions_IndependentConfiguration_Works()
        {
            // Arrange & Act
            var options = new DataLayerOptions
            {
                GenerateInterface = true,
                GenerateGetAll = true,
                GenerateGetById = false,
                GenerateAdd = true,
                GenerateUpdate = false,
                GenerateDelete = true
            };

            // Assert - Verify mixed configuration is valid
            options.GenerateInterface.Should().BeTrue();
            options.GenerateGetAll.Should().BeTrue();
            options.GenerateGetById.Should().BeFalse();
            options.GenerateAdd.Should().BeTrue();
            options.GenerateUpdate.Should().BeFalse();
            options.GenerateDelete.Should().BeTrue();
        }
    }
}