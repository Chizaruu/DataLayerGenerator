using FluentAssertions;
using DataLayerGenerator.Options;
using DataLayerGenerator.Services;
using DataLayerGenerator.UI;
using Xunit;

namespace DataLayerGenerator.Tests.UI
{
    /// <summary>
    /// Tests for GenerateDataLayerDialog
    /// Note: WPF dialogs require STA thread
    /// </summary>
    public class GenerateDataLayerDialogTests
    {
        [StaFact]
        public void Constructor_InitializesWithModelName()
        {
            // Arrange & Act
            var options = new GeneratorOptions();
            var dialog = new GenerateDataLayerDialog("Product", options);

            // Assert
            dialog.Should().NotBeNull();
        }

        [StaFact]
        public void Constructor_SetsDefaultOptions()
        {
            // Arrange
            var options = new GeneratorOptions
            {
                GenerateInterfaces = true,
                GenerateGetAll = true,
                GenerateGetById = true,
                GenerateAdd = true,
                GenerateUpdate = true,
                GenerateDelete = true
            };

            // Act
            var dialog = new GenerateDataLayerDialog("Customer", options);

            // Assert
            dialog.Should().NotBeNull();
            // Dialog should initialize checkboxes with option values
        }

        [StaTheory]
        [InlineData("Product")]
        [InlineData("Customer")]
        [InlineData("Order")]
        [InlineData("BlogPost")]
        public void Constructor_AcceptsDifferentModelNames(string modelName)
        {
            // Arrange
            var options = new GeneratorOptions();

            // Act
            var dialog = new GenerateDataLayerDialog(modelName, options);

            // Assert
            dialog.Should().NotBeNull();
        }

        [StaFact]
        public void GetOptions_ReturnsDataLayerOptions()
        {
            // Arrange
            var options = new GeneratorOptions();
            var dialog = new GenerateDataLayerDialog("Product", options);

            // Act
            var dataOptions = dialog.GetOptions();

            // Assert
            dataOptions.Should().NotBeNull();
            dataOptions.Should().BeOfType<DataLayerOptions>();
        }

        [StaFact]
        public void GetOptions_ReflectsDefaultSelections()
        {
            // Arrange
            var options = new GeneratorOptions
            {
                GenerateInterfaces = true,
                GenerateGetAll = true,
                GenerateGetById = false,
                GenerateAdd = true,
                GenerateUpdate = false,
                GenerateDelete = true
            };

            var dialog = new GenerateDataLayerDialog("Product", options);

            // Act
            var dataOptions = dialog.GetOptions();

            // Assert
            dataOptions.GenerateInterface.Should().BeTrue();
            dataOptions.GenerateGetAll.Should().BeTrue();
            dataOptions.GenerateGetById.Should().BeFalse();
            dataOptions.GenerateAdd.Should().BeTrue();
            dataOptions.GenerateUpdate.Should().BeFalse();
            dataOptions.GenerateDelete.Should().BeTrue();
        }
    }

    /// <summary>
    /// Tests for DataLayerOptions model
    /// </summary>
    public class DataLayerOptionsTests
    {
        [Fact]
        public void DataLayerOptions_DefaultValues_AreCorrect()
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
        public void DataLayerOptions_CanSetCustomValues()
        {
            // Arrange & Act
            var options = new DataLayerOptions
            {
                GenerateInterface = false,
                GenerateGetAll = true,
                GenerateGetById = false,
                GenerateAdd = true,
                GenerateUpdate = false,
                GenerateDelete = true,
                GenerateCustomQueries = true
            };

            // Assert
            options.GenerateInterface.Should().BeFalse();
            options.GenerateGetAll.Should().BeTrue();
            options.GenerateGetById.Should().BeFalse();
            options.GenerateAdd.Should().BeTrue();
            options.GenerateUpdate.Should().BeFalse();
            options.GenerateDelete.Should().BeTrue();
            options.GenerateCustomQueries.Should().BeTrue();
        }

        [Fact]
        public void DataLayerOptions_AllMethodsEnabled_IsValid()
        {
            // Arrange & Act
            var options = new DataLayerOptions
            {
                GenerateGetAll = true,
                GenerateGetById = true,
                GenerateAdd = true,
                GenerateUpdate = true,
                GenerateDelete = true
            };

            // Assert
            options.GenerateGetAll.Should().BeTrue();
            options.GenerateGetById.Should().BeTrue();
            options.GenerateAdd.Should().BeTrue();
            options.GenerateUpdate.Should().BeTrue();
            options.GenerateDelete.Should().BeTrue();
        }

        [Fact]
        public void DataLayerOptions_OnlyReadMethods_IsValid()
        {
            // Arrange & Act
            var options = new DataLayerOptions
            {
                GenerateGetAll = true,
                GenerateGetById = true,
                GenerateAdd = false,
                GenerateUpdate = false,
                GenerateDelete = false
            };

            // Assert
            options.GenerateGetAll.Should().BeTrue();
            options.GenerateGetById.Should().BeTrue();
            options.GenerateAdd.Should().BeFalse();
            options.GenerateUpdate.Should().BeFalse();
            options.GenerateDelete.Should().BeFalse();
        }

        [Fact]
        public void DataLayerOptions_OnlyWriteMethods_IsValid()
        {
            // Arrange & Act
            var options = new DataLayerOptions
            {
                GenerateGetAll = false,
                GenerateGetById = false,
                GenerateAdd = true,
                GenerateUpdate = true,
                GenerateDelete = true
            };

            // Assert
            options.GenerateGetAll.Should().BeFalse();
            options.GenerateGetById.Should().BeFalse();
            options.GenerateAdd.Should().BeTrue();
            options.GenerateUpdate.Should().BeTrue();
            options.GenerateDelete.Should().BeTrue();
        }
    }
}