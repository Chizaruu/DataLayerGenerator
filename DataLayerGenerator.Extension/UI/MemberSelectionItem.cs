using System.ComponentModel;

namespace DataLayerGenerator.UI
{
    public class MemberSelectionItem : INotifyPropertyChanged
    {
        private bool _isSelected;

        public string DisplayText { get; set; }
        public string Signature { get; set; }
        public string MemberType { get; set; }
        public string Constraints { get; set; }

        public bool IsSelected
        {
            get => _isSelected;
            set
            {
                if (_isSelected != value)
                {
                    _isSelected = value;
                    OnPropertyChanged(nameof(IsSelected));
                }
            }
        }

        public event PropertyChangedEventHandler PropertyChanged;

        protected virtual void OnPropertyChanged(string propertyName)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}