using System;
using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace SpendSmart.Models
{
    public enum TransactionType
    {
        Expense,
        Income
    }

    public class Expense : INotifyPropertyChanged
    {
        private string name = string.Empty;
        private double amount;
        private string category = "General";
        private TransactionType type = TransactionType.Expense;
        private DateTime date = DateTime.Now;

        public string Name
        {
            get => name;
            set => SetField(ref name, value);
        }

        public double Amount
        {
            get => amount;
            set => SetField(ref amount, value);
        }

        public string Category
        {
            get => category;
            set => SetField(ref category, value);
        }

        public TransactionType Type
        {
            get => type;
            set => SetField(ref type, value);
        }

        public DateTime Date
        {
            get => date;
            set => SetField(ref date, value);
        }

        public override string ToString()
        {
            return $"{Name} - {Category} | {Type} | ${Amount:F2} on {Date:d}";
        }

        public event PropertyChangedEventHandler? PropertyChanged;

        protected bool SetField<T>(ref T field, T value, [CallerMemberName] string? propertyName = null)
        {
            if (Equals(field, value))
                return false;
            field = value;
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
            return true;
        }
    }
}
