using System.ComponentModel;

namespace WpfApp1.Models
{
    public class Item : INotifyPropertyChanged
    {
        private string _name = "";
        private int _qty;
        private decimal _price;

        public string Name
        {
            get => _name;
            set
            {
                if (_name != value)
                {
                    _name = value;
                    PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(Name)));
                }
            }
        }

        public int Qty
        {
            get => _qty;
            set
            {
                if (_qty != value)
                {
                    _qty = value;
                    PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(Qty)));
                }
            }
        }

        public decimal Price
        {
            get => _price;
            set
            {
                if (_price != value)
                {
                    _price = value;
                    PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(Price)));
                }
            }
        }

        public event PropertyChangedEventHandler? PropertyChanged;
    }
}