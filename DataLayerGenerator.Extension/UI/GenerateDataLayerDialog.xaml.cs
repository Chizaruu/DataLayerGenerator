using DataLayerGenerator.Options;
using DataLayerGenerator.Services;
using System.Windows;

namespace DataLayerGenerator.UI
{
    public partial class GenerateDataLayerDialog : Window
    {
        private readonly GeneratorOptions _options;

        public GenerateDataLayerDialog(string modelName, GeneratorOptions options)
        {
            InitializeComponent();

            _options = options;

            ModelNameText.Text = modelName;

            // Set default values from options
            GenerateInterfaceCheckBox.IsChecked = _options.GenerateInterfaces;
            GenerateGetAllCheckBox.IsChecked = _options.GenerateGetAll;
            GenerateGetByIdCheckBox.IsChecked = _options.GenerateGetById;
            GenerateAddCheckBox.IsChecked = _options.GenerateAdd;
            GenerateUpdateCheckBox.IsChecked = _options.GenerateUpdate;
            GenerateDeleteCheckBox.IsChecked = _options.GenerateDelete;
            IncludeCustomQueriesCheckBox.IsChecked = _options.IncludeCustomQueryPlaceholder;
        }

        public DataLayerOptions GetOptions()
        {
            return new DataLayerOptions
            {
                GenerateInterface = GenerateInterfaceCheckBox.IsChecked == true,
                GenerateGetAll = GenerateGetAllCheckBox.IsChecked == true,
                GenerateGetById = GenerateGetByIdCheckBox.IsChecked == true,
                GenerateAdd = GenerateAddCheckBox.IsChecked == true,
                GenerateUpdate = GenerateUpdateCheckBox.IsChecked == true,
                GenerateDelete = GenerateDeleteCheckBox.IsChecked == true,
                GenerateCustomQueries = IncludeCustomQueriesCheckBox.IsChecked == true
            };
        }

        private void OK_Click(object sender, RoutedEventArgs e)
        {
            // Validate that at least one method is selected
            if (GenerateGetAllCheckBox.IsChecked != true &&
                GenerateGetByIdCheckBox.IsChecked != true &&
                GenerateAddCheckBox.IsChecked != true &&
                GenerateUpdateCheckBox.IsChecked != true &&
                GenerateDeleteCheckBox.IsChecked != true)
            {
                MessageBox.Show(
                    "Please select at least one method to generate.",
                    Constants.ExtensionName,
                    MessageBoxButton.OK,
                    MessageBoxImage.Warning);
                return;
            }

            DialogResult = true;
            Close();
        }

        private void Cancel_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = false;
            Close();
        }

        private void SelectAll_Click(object sender, RoutedEventArgs e)
        {
            GenerateGetAllCheckBox.IsChecked = true;
            GenerateGetByIdCheckBox.IsChecked = true;
            GenerateAddCheckBox.IsChecked = true;
            GenerateUpdateCheckBox.IsChecked = true;
            GenerateDeleteCheckBox.IsChecked = true;
        }

        private void DeselectAll_Click(object sender, RoutedEventArgs e)
        {
            GenerateGetAllCheckBox.IsChecked = false;
            GenerateGetByIdCheckBox.IsChecked = false;
            GenerateAddCheckBox.IsChecked = false;
            GenerateUpdateCheckBox.IsChecked = false;
            GenerateDeleteCheckBox.IsChecked = false;
        }
    }
}
