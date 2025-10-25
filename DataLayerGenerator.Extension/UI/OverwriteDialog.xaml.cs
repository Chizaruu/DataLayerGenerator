using System.Windows;

namespace DataLayerGenerator.UI
{
    public partial class OverwriteDialog : Window
    {
        public OverwriteChoice Choice { get; private set; }

        public OverwriteDialog(string fileName)
        {
            InitializeComponent();
            MessageText.Text = $"File '{fileName}' already exists.";
            Choice = OverwriteChoice.No;
        }

        private void Yes_Click(object sender, RoutedEventArgs e)
        {
            Choice = OverwriteChoice.Yes;
            DialogResult = true;
            Close();
        }

        private void YesToAll_Click(object sender, RoutedEventArgs e)
        {
            Choice = OverwriteChoice.YesToAll;
            DialogResult = true;
            Close();
        }

        private void No_Click(object sender, RoutedEventArgs e)
        {
            Choice = OverwriteChoice.No;
            DialogResult = false;
            Close();
        }

        private void NoToAll_Click(object sender, RoutedEventArgs e)
        {
            Choice = OverwriteChoice.NoToAll;
            DialogResult = false;
            Close();
        }
    }

    /// <summary>
    /// Represents the user's choice for overwriting files
    /// </summary>
    public enum OverwriteChoice
    {
        Ask,
        Yes,
        YesToAll,
        No,
        NoToAll
    }
}