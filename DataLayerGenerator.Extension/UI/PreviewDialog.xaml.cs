using System.Windows;

namespace DataLayerGenerator.UI
{
    public partial class PreviewDialog : Window
    {
        public bool UserApproved { get; private set; }

        public PreviewDialog(string interfaceName, string filePath, string interfaceCode)
        {
            InitializeComponent();

            InterfaceNameText.Text = interfaceName;
            FilePathText.Text = filePath;
            PreviewTextBox.Text = interfaceCode;

            UserApproved = false;
        }

        private void Save_Click(object sender, RoutedEventArgs e)
        {
            UserApproved = true;
            DialogResult = true;
            Close();
        }

        private void Cancel_Click(object sender, RoutedEventArgs e)
        {
            UserApproved = false;
            DialogResult = false;
            Close();
        }
    }
}