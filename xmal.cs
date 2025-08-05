using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Printing;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Media;
using WpfApp1.Models;

namespace WpfApp1
{
    public partial class MainWindow : Window, INotifyPropertyChanged
    {
        public event PropertyChangedEventHandler? PropertyChanged;

        private string _invoiceDate = "";
        private string _searchText = "";
        private decimal _subtotal;
        private decimal _discount;
        private decimal _total;
        private string _savingsText = "You have saved ₹0.00";
        private int _billNumber = 1;
        private string _currentTime = "";
        private string _windowTitle = "AAPNA CHEMIST - Billing System";

        public string WindowTitle
        {
            get => _windowTitle;
            set
            {
                if (_windowTitle != value)
                {
                    _windowTitle = value;
                    PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(WindowTitle)));
                }
            }
        }

        public string InvoiceDate
        {
            get => _invoiceDate;
            set
            {
                if (_invoiceDate != value)
                {
                    _invoiceDate = value;
                    PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(InvoiceDate)));
                }
            }
        }

        public string SearchText
        {
            get => _searchText;
            set
            {
                if (_searchText != value)
                {
                    _searchText = value;
                    PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(SearchText)));
                    FilterItems();
                }
            }
        }

        public decimal Subtotal
        {
            get => _subtotal;
            set
            {
                if (_subtotal != value)
                {
                    _subtotal = value;
                    PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(Subtotal)));
                    UpdateTotal();
                }
            }
        }

        public decimal Discount
        {
            get => _discount;
            set
            {
                if (_discount != value)
                {
                    _discount = value;
                    PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(Discount)));
                    UpdateTotal();
                }
            }
        }

        public decimal Total
        {
            get => _total;
            set
            {
                if (_total != value)
                {
                    _total = value;
                    PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(Total)));
                }
            }
        }

        public string SavingsText
        {
            get => _savingsText;
            set
            {
                if (_savingsText != value)
                {
                    _savingsText = value;
                    PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(SavingsText)));
                }
            }
        }

        public int BillNumber
        {
            get => _billNumber;
            set
            {
                if (_billNumber != value)
                {
                    _billNumber = value;
                    PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(BillNumber)));
                    UpdateWindowTitle();
                }
            }
        }

        public string CurrentTime
        {
            get => _currentTime;
            set
            {
                if (_currentTime != value)
                {
                    _currentTime = value;
                    PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(CurrentTime)));
                }
            }
        }

        public ObservableCollection<Item> Items { get; set; } = new ObservableCollection<Item>();
        public ObservableCollection<Item> FilteredItems { get; set; } = new ObservableCollection<Item>();
        public ObservableCollection<BillItem> BillItems { get; set; } = new ObservableCollection<BillItem>();

        public MainWindow()
        {
            InitializeComponent();
            DataContext = this;
            LoadItems();
            ItemsDataGrid.ItemsSource = FilteredItems;
            BillDataGrid.ItemsSource = BillItems;
            UpdateTotals();
            InvoiceDate = DateTime.Now.ToString("dd/MM/yy");
            CurrentTime = DateTime.Now.ToString("hh:mm tt");
            UpdateWindowTitle();
        }

        private void LoadItems()
        {
            Items.Clear();

            // Try multiple possible paths for the CSV file
            string[] possiblePaths = {
                "updated_medicine_list.csv",
                Path.Combine(Directory.GetCurrentDirectory(), "updated_medicine_list.csv"),
                Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "updated_medicine_list.csv"),
                @"C:\Users\student\source\repos\Billing-System\Billing System\WpfApp1\updated_medicine_list.csv"
            };

            string csvPath = null;
            foreach (string path in possiblePaths)
            {
                if (File.Exists(path))
                {
                    csvPath = path;
                    break;
                }
            }

            if (csvPath == null)
            {
                MessageBox.Show("Dataset file 'updated_medicine_list.csv' not found. Please ensure the file is in the application directory.",
                    "File Not Found", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            try
            {
                var lines = File.ReadAllLines(csvPath);
                if (lines.Length < 2)
                {
                    MessageBox.Show("CSV file is empty or invalid format.", "Invalid File", MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                foreach (var line in lines.Skip(1)) // Skip header
                {
                    if (string.IsNullOrWhiteSpace(line)) continue;

                    var parts = line.Split(',');
                    if (parts.Length < 3) continue;

                    var name = parts[0].Trim().Trim('"'); // Remove quotes if present
                    if (!int.TryParse(parts[1].Trim(), out int qty)) continue;
                    if (!decimal.TryParse(parts[2].Trim(), NumberStyles.Any, CultureInfo.InvariantCulture, out decimal price)) continue;

                    Items.Add(new Item
                    {
                        Name = name,
                        Qty = qty,
                        Price = price
                    });
                }

                FilteredItems.Clear();
                foreach (var item in Items)
                {
                    FilteredItems.Add(item);
                }

                // Update the items grid
                ItemsDataGrid.Items.Refresh();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error loading CSV file: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void FilterItems()
        {
            FilteredItems.Clear();
            var filtered = Items.Where(item =>
                string.IsNullOrEmpty(SearchText) ||
                item.Name.ToLower().Contains(SearchText.ToLower()));

            foreach (var item in filtered)
            {
                FilteredItems.Add(item);
            }

            // Update the grid
            ItemsDataGrid.Items.Refresh();
        }

        private void AddToBillButton_Click(object sender, RoutedEventArgs e)
        {
            if (ItemsDataGrid.SelectedItem is Item selectedItem)
            {
                if (int.TryParse(QtyTextBox.Text, out int qtyToAdd) && qtyToAdd > 0)
                {
                    if (selectedItem.Qty >= qtyToAdd)
                    {
                        selectedItem.Qty -= qtyToAdd;

                        var billItem = BillItems.FirstOrDefault(b => b.Name == selectedItem.Name);
                        if (billItem == null)
                        {
                            BillItems.Add(new BillItem
                            {
                                Sr = BillItems.Count + 1,
                                Name = selectedItem.Name,
                                Quantity = qtyToAdd,
                                Rate = selectedItem.Price
                            });
                        }
                        else
                        {
                            billItem.Quantity += qtyToAdd;
                        }

                        // Update serial numbers
                        UpdateBillItemSerialNumbers();

                        BillDataGrid.Items.Refresh();
                        ItemsDataGrid.Items.Refresh();
                        UpdateTotals();
                        QtyTextBox.Text = "1";
                    }
                    else
                    {
                        MessageBox.Show($"Insufficient stock. Available: {selectedItem.Qty}", "Stock Error", MessageBoxButton.OK, MessageBoxImage.Warning);
                    }
                }
                else
                {
                    MessageBox.Show("Please enter a valid quantity greater than 0.", "Invalid Quantity", MessageBoxButton.OK, MessageBoxImage.Warning);
                }
            }
            else
            {
                MessageBox.Show("Please select an item from the product list.", "No Item Selected", MessageBoxButton.OK, MessageBoxImage.Information);
            }
        }

        private void UpdateBillItemSerialNumbers()
        {
            for (int i = 0; i < BillItems.Count; i++)
            {
                BillItems[i].Sr = i + 1;
            }
        }

        private void SearchButton_Click(object sender, RoutedEventArgs e)
        {
            FilterItems();
        }

        private void ClearSearchButton_Click(object sender, RoutedEventArgs e)
        {
            SearchText = "";
            FilterItems();
        }

        private void ClearBillButton_Click(object sender, RoutedEventArgs e)
        {
            if (MessageBox.Show("Are you sure you want to clear the bill?", "Confirm Clear",
                MessageBoxButton.YesNo, MessageBoxImage.Question) == MessageBoxResult.Yes)
            {
                BillItems.Clear();
                UpdateTotals();
            }
        }

        private void PrintBillButton_Click(object sender, RoutedEventArgs e)
        {
            if (BillItems.Count == 0)
            {
                MessageBox.Show("No items in bill to print.", "Empty Bill", MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }

            try
            {
                PrintDialog printDialog = new PrintDialog();

                // Configure for receipt printer
                printDialog.PrintTicket.PageMediaSize = new PageMediaSize(PageMediaSizeName.ISOA6); // 80mm width

                if (printDialog.ShowDialog() == true)
                {
                    FlowDocument flowDocument = CreatePrinterFriendlyBillDocument();

                    // Set fixed page size
                    flowDocument.PageWidth = 288; // 80mm in pixels (96 DPI)
                    flowDocument.PagePadding = new Thickness(10);
                    flowDocument.ColumnWidth = double.PositiveInfinity; // Single column

                    // Print the document
                    printDialog.PrintDocument(
                        ((IDocumentPaginatorSource)flowDocument).DocumentPaginator,
                        $"AAPNA CHEMIST Bill #{BillNumber}");

                    // Complete sale after printing
                    CompleteSaleButton_Click(sender, e);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error printing bill: {ex.Message}", "Print Error",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private FlowDocument CreatePrinterFriendlyBillDocument()
        {
            FlowDocument document = new FlowDocument();
            document.FontFamily = new FontFamily("Courier New"); // Monospaced font for alignment
            document.FontSize = 11;
            document.PagePadding = new Thickness(5);

            // Header - AAPNA CHEMIST
            Paragraph header = new Paragraph(new Run("           AAPNA CHEMIST"))
            {
                TextAlignment = TextAlignment.Center,
                FontWeight = FontWeights.Bold,
                FontSize = 12
            };

            // Address
            Paragraph address = new Paragraph();
            address.Inlines.Add(new Run("Hathi Bhai Patel Building, Shop No.1/2\n"));
            address.Inlines.Add(new Run("Bhiwandi-Wada Road, Opp. Police Station\n"));
            address.Inlines.Add(new Run("Kudus - 421312, Palghar Dist.\n"));
            address.Inlines.Add(new Run("GSTIN: 27AEGPG3762F1ZM | Mob: 9890581131"));
            address.TextAlignment = TextAlignment.Center;

            // TAX INVOICE
            Paragraph invoiceTitle = new Paragraph(new Run("TAX INVOICE"))
            {
                TextAlignment = TextAlignment.Center,
                FontWeight = FontWeights.Bold,
                FontSize = 12
            };

            // Separator
            Paragraph separator = new Paragraph(new Run(new string('-', 42)))
            {
                TextAlignment = TextAlignment.Center
            };

            // Bill info
            Paragraph billInfo = new Paragraph();
            billInfo.Inlines.Add(new Run($"Bill No : {BillNumber}           User  : Admin\n"));
            billInfo.Inlines.Add(new Run($"Date    : {InvoiceDate}   Time  : {CurrentTime}\n"));
            billInfo.Inlines.Add(new Run($"Counter : POS"));

            // Items header
            Paragraph itemsHeader = new Paragraph(new Run("Sr  Item Name                         Qty  Rate   Amount"))
            {
                FontWeight = FontWeights.Bold
            };

            // Items separator
            Paragraph itemsSeparator = new Paragraph(new Run(new string('-', 42)))
            {
                TextAlignment = TextAlignment.Center
            };

            // Items
            Section itemsSection = new Section();
            foreach (var item in BillItems)
            {
                // Truncate long item names
                string displayName = item.Name.Length > 25 ?
                    item.Name.Substring(0, 22) + "..." :
                    item.Name.PadRight(25);

                Paragraph itemLine = new Paragraph();
                itemLine.Inlines.Add(new Run(
                    $"{item.Sr,2}  {displayName} {item.Quantity,3}  {item.Rate,5:F2}  {item.Amount,6:F2}"));
                itemsSection.Blocks.Add(itemLine);
            }

            // Totals separator
            Paragraph totalsSeparator = new Paragraph(new Run(new string('-', 42)))
            {
                TextAlignment = TextAlignment.Center
            };

            // Totals info
            int totalQty = BillItems.Sum(b => b.Quantity);
            int totalItems = BillItems.Count;

            Paragraph totalsInfo = new Paragraph();
            totalsInfo.Inlines.Add(new Run($"Total Quantity: {totalQty}\n"));
            totalsInfo.Inlines.Add(new Run($"Total Items  : {totalItems}\n"));
            totalsInfo.Inlines.Add(new Run($"Coin Adjustment : 0.00"));

            // Amounts separator
            Paragraph amountsSeparator = new Paragraph(new Run(new string('-', 42)))
            {
                TextAlignment = TextAlignment.Center
            };

            // Total amounts
            Paragraph totalAmount = new Paragraph();
            totalAmount.Inlines.Add(new Run($"TOTAL                               : ₹ {Total,7:F2}\n"));
            totalAmount.Inlines.Add(new Run($"Cash Received                       : ₹ {Total,7:F2}\n"));
            totalAmount.Inlines.Add(new Run($"Balance                             : ₹ {"0.00",7}"));

            // Savings separator
            Paragraph savingsSeparator = new Paragraph(new Run(new string('-', 42)))
            {
                TextAlignment = TextAlignment.Center
            };

            // Savings
            decimal savings = Subtotal * 0.10m;
            Paragraph savingsPara = new Paragraph(new Run($"        You have saved ₹ {savings:F2}"))
            {
                TextAlignment = TextAlignment.Center,
                FontWeight = FontWeights.Bold
            };

            // Footer separator
            Paragraph footerSeparator = new Paragraph(new Run(new string('-', 42)))
            {
                TextAlignment = TextAlignment.Center
            };

            // Footer
            Paragraph footer = new Paragraph();
            footer.Inlines.Add(new Run("         THANK YOU..! VISIT AGAIN..!\n"));
            footer.Inlines.Add(new Run("     RETURNS ACCEPTED WITHIN 7 DAYS ONLY"));
            footer.TextAlignment = TextAlignment.Center;
            footer.FontWeight = FontWeights.Bold;

            // Add all elements to document
            document.Blocks.Add(header);
            document.Blocks.Add(address);
            document.Blocks.Add(invoiceTitle);
            document.Blocks.Add(separator);
            document.Blocks.Add(billInfo);
            document.Blocks.Add(separator);
            document.Blocks.Add(itemsHeader);
            document.Blocks.Add(itemsSeparator);
            document.Blocks.Add(itemsSection);
            document.Blocks.Add(totalsSeparator);
            document.Blocks.Add(totalsInfo);
            document.Blocks.Add(amountsSeparator);
            document.Blocks.Add(totalAmount);
            document.Blocks.Add(savingsSeparator);
            document.Blocks.Add(savingsPara);
            document.Blocks.Add(footerSeparator);
            document.Blocks.Add(footer);

            return document;
        }

        private void CompleteSaleButton_Click(object sender, RoutedEventArgs e)
        {
            if (BillItems.Count == 0)
            {
                MessageBox.Show("No items in bill to complete sale.", "Empty Bill", MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }

            var result = MessageBox.Show($"Complete sale for ₹{Total:F2}?", "Confirm Sale",
                MessageBoxButton.YesNo, MessageBoxImage.Question);

            if (result == MessageBoxResult.Yes)
            {
                try
                {
                    // Save the sale to a file or database
                    SaveSaleToFile();

                    MessageBox.Show($"Sale completed successfully!\nTotal Amount: ₹{Total:F2}",
                        "Sale Complete", MessageBoxButton.OK, MessageBoxImage.Information);

                    // Clear the bill and increment bill number
                    BillItems.Clear();
                    BillNumber++;
                    CurrentTime = DateTime.Now.ToString("hh:mm tt");
                    UpdateTotals();
                    UpdateWindowTitle();
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Error completing sale: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
        }

        private void SaveSaleToFile()
        {
            try
            {
                string salesLogPath = "sales_log.txt";
                string saleRecord = $"{DateTime.Now:yyyy-MM-dd HH:mm:ss} | Bill #{BillNumber} | Total: ₹{Total:F2} | Items: {BillItems.Count}\n";
                File.AppendAllText(salesLogPath, saleRecord);
            }
            catch (Exception ex)
            {
                // Log error but don't fail the sale
                System.Diagnostics.Debug.WriteLine($"Error saving sale: {ex.Message}");
            }
        }

        private void UpdateTotals()
        {
            Subtotal = BillItems.Sum(b => b.Amount);
            Total = Subtotal - Discount;

            // Calculate savings (example: 10% discount)
            decimal savings = Subtotal * 0.10m;
            SavingsText = $"You have saved ₹{savings:F2}";
        }

        private void UpdateTotal()
        {
            Total = Subtotal - Discount;
        }

        private void UpdateWindowTitle()
        {
            WindowTitle = $"AAPNA CHEMIST - Bill #{BillNumber} - Billing System";
        }

        private void PreviewBillButton_Click(object sender, RoutedEventArgs e)
        {
            if (BillItems.Count == 0)
            {
                MessageBox.Show("No items in bill to preview.", "Empty Bill", MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }

            try
            {
                // Create a preview document
                FlowDocument previewDoc = CreatePrinterFriendlyBillDocument();

                // Show print preview dialog
                PrintPreviewWindow previewWindow = new PrintPreviewWindow(previewDoc);
                previewWindow.Title = $"Bill #{BillNumber} Preview";
                previewWindow.ShowDialog();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error generating bill preview: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
    }

    public class BillItem : INotifyPropertyChanged
    {
        private int _sr;
        private string _name = "";
        private int _quantity;
        private decimal _rate;

        public int Sr
        {
            get => _sr;
            set
            {
                if (_sr != value)
                {
                    _sr = value;
                    PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(Sr)));
                }
            }
        }

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

        public int Quantity
        {
            get => _quantity;
            set
            {
                if (_quantity != value)
                {
                    _quantity = value;
                    PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(Quantity)));
                    PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(Amount)));
                }
            }
        }

        public decimal Rate
        {
            get => _rate;
            set
            {
                if (_rate != value)
                {
                    _rate = value;
                    PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(Rate)));
                    PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(Amount)));
                }
            }
        }

        public decimal Amount => Rate * Quantity;

        public event PropertyChangedEventHandler? PropertyChanged;
    }

    // Simple print preview window class
    public class PrintPreviewWindow : Window
    {
        public PrintPreviewWindow(FlowDocument document)
        {
            Title = "Print Preview";
            Width = 400;
            Height = 600;
            WindowStartupLocation = WindowStartupLocation.CenterOwner;

            // Create document viewer
            DocumentViewer viewer = new DocumentViewer();
            viewer.Document = document;
            Content = viewer;

            // Add print button
            Button printButton = new Button
            {
                Content = "Print",
                HorizontalAlignment = HorizontalAlignment.Right,
                VerticalAlignment = VerticalAlignment.Bottom,
                Margin = new Thickness(10),
                Padding = new Thickness(5)
            };
            printButton.Click += (s, e) =>
            {
                PrintDialog printDialog = new PrintDialog();
                if (printDialog.ShowDialog() == true)
                {
                    printDialog.PrintDocument(
                        ((IDocumentPaginatorSource)document).DocumentPaginator,
                        "Bill Print");
                }
            };

            // Create container grid
            Grid grid = new Grid();
            grid.Children.Add(viewer);
            grid.Children.Add(printButton);
            Content = grid;
        }
    }
}
