using Avalonia.Controls;
using Avalonia;
using Avalonia.Interactivity;
using Avalonia.Media;
using SpendSmart.Models;
using System;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text.Json;

namespace SpendSmart
{
    public partial class MainWindow : Window
    {
        private ObservableCollection<Expense> expenses = new();
        private Expense? editingExpense = null;
        private const double SpendingLimit = 1000.00;

        public MainWindow()
        {
            InitializeComponent();

            CategoryBox.SelectedIndex = 0;
            TypeBox.SelectedIndex = 0;
            ExpensesList.ItemsSource = expenses;

            expenses.CollectionChanged += (s, e) =>
            {
                UpdateTotal();
                UpdateChartFiles();
            };

            UpdateTotal();
            UpdateChartFiles();
        }

        private void OnAddExpenseClicked(object? sender, RoutedEventArgs e)
        {
            if (string.IsNullOrWhiteSpace(ExpenseNameBox.Text))
            {
                ShowMessage("Please enter an expense name.");
                return;
            }

            if (!double.TryParse(AmountBox.Text, out var amount) || amount < 0)
            {
                ShowMessage("Please enter a valid positive amount.");
                return;
            }

            var category = (CategoryBox.SelectedItem as ComboBoxItem)?.Content?.ToString() ?? "General";
            var typeString = (TypeBox.SelectedItem as ComboBoxItem)?.Content?.ToString() ?? "Expense";
            var type = Enum.TryParse<TransactionType>(typeString, out var parsedType) ? parsedType : TransactionType.Expense;

            if (type == TransactionType.Expense && editingExpense == null)
            {
                var currentExpenses = expenses.Where(e => e.Type == TransactionType.Expense).Sum(e => e.Amount);
                if (currentExpenses + amount > SpendingLimit)
                {
                    ShowMessage($"Spending limit of ${SpendingLimit} exceeded! Please visit your bank to increase your limit.");
                    return;
                }
            }

            if (editingExpense != null)
            {
                editingExpense.Name = ExpenseNameBox.Text;
                editingExpense.Amount = amount;
                editingExpense.Category = category;
                editingExpense.Type = type;

                ExpensesList.ItemsSource = null!;
                ExpensesList.ItemsSource = expenses;

                editingExpense = null;
                CancelEditButton.IsVisible = false;
                AddButton.Content = "Add";
            }
            else
            {
                expenses.Add(new Expense
                {
                    Name = ExpenseNameBox.Text,
                    Amount = amount,
                    Category = category,
                    Type = type,
                    Date = DateTime.Now
                });
            }

            ClearInputFields();
            UpdateTotal();
            UpdateChartFiles();
        }

        private void OnRemoveExpenseClicked(object? sender, RoutedEventArgs e)
        {
            if (ExpensesList.SelectedItem is Expense selected)
            {
                expenses.Remove(selected);
                ClearInputFields();
                editingExpense = null;
                CancelEditButton.IsVisible = false;
                AddButton.Content = "Add";
            }
            else
            {
                ShowMessage("Please select an expense to remove.");
            }
        }

        private void ClearInputFields()
        {
            ExpenseNameBox.Text = "";
            AmountBox.Text = "";
            CategoryBox.SelectedIndex = 0;
            TypeBox.SelectedIndex = 0;
        }

        private void UpdateTotal()
        {
            double balance = expenses.Sum(exp => exp.Type == TransactionType.Income ? exp.Amount : -exp.Amount);
            TotalTextBlock.Text = $"Balance: ${balance:F2}";
            TotalTextBlock.Foreground = balance < 0 ? Brushes.Red : Brushes.White;
        }

        private async void ShowMessage(string message)
        {
            var dialog = new Window
            {
                Title = "Alert",
                Width = 300,
                Height = 150,
                WindowStartupLocation = WindowStartupLocation.CenterOwner,
                CanResize = false
            };

            var okButton = new Button
            {
                Content = "OK",
                HorizontalAlignment = Avalonia.Layout.HorizontalAlignment.Center,
                Width = 60,
                Margin = new Thickness(0, 10, 0, 0),
            };

            okButton.Click += (_, _) => dialog.Close();

            var stackPanel = new StackPanel
            {
                Children =
                {
                    new TextBlock
                    {
                        Text = message,
                        Margin = new Thickness(20),
                        TextWrapping = TextWrapping.Wrap,
                        HorizontalAlignment = Avalonia.Layout.HorizontalAlignment.Center,
                        VerticalAlignment = Avalonia.Layout.VerticalAlignment.Center
                    },
                    okButton
                }
            };

            dialog.Content = stackPanel;
            await dialog.ShowDialog(this);
        }

        private void ExpensesList_SelectionChanged(object? sender, SelectionChangedEventArgs e)
        {
            if (ExpensesList.SelectedItem is Expense exp)
            {
                editingExpense = exp;

                ExpenseNameBox.Text = exp.Name;
                AmountBox.Text = exp.Amount.ToString();

                var categoryIndex = CategoryBox.Items
                    .Cast<ComboBoxItem>()
                    .Select((item, index) => new { item, index })
                    .FirstOrDefault(x => (string?)x.item.Content == exp.Category)?.index ?? 0;
                CategoryBox.SelectedIndex = categoryIndex;

                var typeIndex = TypeBox.Items
                    .Cast<ComboBoxItem>()
                    .Select((item, index) => new { item, index })
                    .FirstOrDefault(x => (string?)x.item.Content == exp.Type.ToString())?.index ?? 0;
                TypeBox.SelectedIndex = typeIndex;

                CancelEditButton.IsVisible = true;
                AddButton.Content = "Update";
            }
        }

        private void OnCreateChartClicked(object? sender, RoutedEventArgs e)
        {
            UpdateChartFiles();
        }

        private void OnOpenChartClicked(object? sender, RoutedEventArgs e) => OpenChartInBrowser();

        private void OpenChartInBrowser()
        {
            var chartPath = Path.Combine(AppContext.BaseDirectory, "chart.html");
            if (File.Exists(chartPath))
            {
                var url = new Uri(chartPath).AbsoluteUri;
                Process.Start(new ProcessStartInfo { FileName = url, UseShellExecute = true });
            }
            else
            {
                ShowMessage("Chart file not found.");
            }
        }

        private void OnCancelEditClicked(object? sender, RoutedEventArgs e)
        {
            ClearInputFields();
            editingExpense = null;
            CancelEditButton.IsVisible = false;
            AddButton.Content = "Add";

            ExpensesList.SelectedItem = null;
        }

        private void UpdateChartFiles()
        {
            var groupedExpenses = expenses.Where(e => e.Type == TransactionType.Expense)
                .GroupBy(e => e.Category)
                .Select(g => new { category = g.Key, amount = g.Sum(e => e.Amount) })
                .ToList();

            double totalExpenses = expenses.Where(e => e.Type == TransactionType.Expense).Sum(e => e.Amount);
            double totalIncome = expenses.Where(e => e.Type == TransactionType.Income).Sum(e => e.Amount);

            var chartData = new
            {
                expenses = groupedExpenses.Select(g => new { category = g.category, amount = g.amount }),
                totalExpenses = totalExpenses,
                totalIncome = totalIncome
            };

            var json = JsonSerializer.Serialize(chartData, new JsonSerializerOptions { WriteIndented = true });
            var jsonPath = Path.Combine(AppContext.BaseDirectory, "chart-data.json");
            File.WriteAllText(jsonPath, json);
        }


        private string GetChartHtmlContent()
        {
            return @"<!DOCTYPE html>
<html lang=""en"">
<head>
    <meta charset=""UTF-8"">
    <meta name=""viewport"" content=""width=device-width, initial-scale=1.0"">
    <title>Spending by Category</title>
    <script src=""https://cdn.jsdelivr.net/npm/chart.js""></script>
    <style>
        body {
            font-family: sans-serif;
            background: #1e1e1e;
            color: white;
            text-align: center;
            padding: 20px;
        }
        canvas {
            max-width: 600px;
            margin: auto;
        }
    </style>
</head>
<body>
    <h2>Spending by Category</h2>
    <canvas id=""spendingChart""></canvas>

    <script>
        async function loadChartData() {
            try {
                // Added cache-busting query param to avoid caching issues
                const response = await fetch('chart-data.json?' + Date.now());
                const data = await response.json();

                if (!data.expenses || data.expenses.length === 0) {
                    createChart([], []);
                    return;
                }

                const labels = data.expenses.map(e => e.category);
                const amounts = data.expenses.map(e => e.amount);

                createChart(labels, amounts);
            } catch (error) {
                console.error('Error loading chart data:', error);
                createChart([], []);
            }
        }

        function createChart(labels, data) {
            if (window.myChart) {
                window.myChart.destroy();
            }
            window.myChart = new Chart(document.getElementById('spendingChart'), {
                type: 'pie',
                data: {
                    labels: labels,
                    datasets: [{
                        label: 'Expenses',
                        data: data,
                        backgroundColor: [
                            'rgba(255, 99, 132, 0.7)',
                            'rgba(54, 162, 235, 0.7)',
                            'rgba(255, 206, 86, 0.7)',
                            'rgba(75, 192, 192, 0.7)',
                            'rgba(153, 102, 255, 0.7)',
                            'rgba(255, 159, 64, 0.7)'
                        ],
                        borderColor: 'rgba(0,0,0,0.2)',
                        borderWidth: 1
                    }]
                },
                options: {
                    responsive: true,
                    plugins: {
                        legend: {
                            position: 'bottom',
                            labels: {
                                color: 'white'
                            }
                        },
                        tooltip: {
                            enabled: true
                        }
                    }
                }
            });
        }

        loadChartData();
    </script>
</body>
</html>";
        }

    }
}
