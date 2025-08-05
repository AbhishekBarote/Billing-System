using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Xps;
using System.Windows.Xps.Packaging;
using System.Windows.Xps.Serialization;
using System.Printing;
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
                @"E:\Billing System\WpfApp1\updated_medicine_list.csv"
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
                // Show print dialog
                PrintDialog printDialog = new PrintDialog();
                if (printDialog.ShowDialog() == true)
                {
                    // Create a FlowDocument for printing
                    FlowDocument flowDocument = CreateBillDocument();
                    
                    // Set the document to print
                    DocumentPaginator paginator = ((IDocumentPaginatorSource)flowDocument).DocumentPaginator;
                    
                    // Print the document
                    printDialog.PrintDocument(paginator, "AAPNA CHEMIST Bill");
                    
                    MessageBox.Show("Bill printed successfully!", "Print Complete", MessageBoxButton.OK, MessageBoxImage.Information);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error printing bill: {ex.Message}", "Print Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private FlowDocument CreateBillDocument()
        {
            FlowDocument document = new FlowDocument();
            document.PageWidth = 400;
            document.PageHeight = 600;
            document.PagePadding = new Thickness(20);

            // Create the document content
            Section section = new Section();
            
            // Header - AAPNA CHEMIST (centered)
            Paragraph header = new Paragraph();
            header.TextAlignment = TextAlignment.Center;
            header.FontSize = 16;
            header.FontWeight = FontWeights.Bold;
            header.Inlines.Add(new Run("                 AAPNA CHEMIST"));
            
            // Address (centered)
            Paragraph address = new Paragraph();
            address.TextAlignment = TextAlignment.Center;
            address.FontSize = 10;
            address.Inlines.Add(new Run("Hathi Bhai Patel Building, Shop No.1/2, Dist.Palghar"));
            address.Inlines.Add(new LineBreak());
            address.Inlines.Add(new Run("Bhiwandi, Wada Road, Opp.Police Stn, Kudus-421312."));
            address.Inlines.Add(new LineBreak());
            address.Inlines.Add(new Run("GST NO: 27AEGPG3762F1ZM   Mob: 9890581131"));
            
            // INVOICE header
            Paragraph invoiceHeader = new Paragraph();
            invoiceHeader.TextAlignment = TextAlignment.Center;
            invoiceHeader.FontSize = 14;
            invoiceHeader.FontWeight = FontWeights.Bold;
            invoiceHeader.Inlines.Add(new Run("INVOICE"));
            
            // Bill Info
            Paragraph billInfo = new Paragraph();
            billInfo.FontSize = 11;
            billInfo.Inlines.Add(new Run($"Bill #: {BillNumber}    User: Admin     Date: {InvoiceDate}"));
            billInfo.Inlines.Add(new LineBreak());
            billInfo.Inlines.Add(new Run($"Counter: POS  Time: {CurrentTime}"));
            
            // Separator line
            Paragraph separator = new Paragraph();
            separator.Inlines.Add(new Run(new string('-', 50)));
            
            // Item header
            Paragraph itemHeader = new Paragraph();
            itemHeader.FontSize = 11;
            itemHeader.FontWeight = FontWeights.Bold;
            itemHeader.Inlines.Add(new Run("Sr  Item Name        Qty  MRP   Rate   Amount"));
            itemHeader.Inlines.Add(new LineBreak());
            itemHeader.Inlines.Add(new Run(new string('-', 50)));
            
            // Items
            foreach (var item in BillItems)
            {
                Paragraph itemLine = new Paragraph();
                itemLine.FontSize = 10;
                itemLine.Inlines.Add(new Run($"{item.Sr,2}   {item.Name,-15} {item.Quantity,3}  {item.Rate,4}  {item.Rate,5}  {item.Amount,7:F2}"));
                
                section.Blocks.Add(itemLine);
            }
            
            // Separator line
            Paragraph totalSeparator = new Paragraph();
            totalSeparator.Inlines.Add(new Run(new string('-', 50)));
            
            // Calculate totals
            int totalQty = BillItems.Sum(b => b.Quantity);
            int totalItems = BillItems.Count;
            decimal coinAdj = 0.00m;
            
            // Totals section
            Paragraph totalsInfo = new Paragraph();
            totalsInfo.FontSize = 11;
            totalsInfo.Inlines.Add(new Run($"Tot Qty: {totalQty}   Tot Items: {totalItems}   Coin Adj: {coinAdj:F2}"));
            totalsInfo.Inlines.Add(new LineBreak());
            totalsInfo.Inlines.Add(new LineBreak());
            
            // Total amount
            Paragraph total = new Paragraph();
            total.FontSize = 12;
            total.FontWeight = FontWeights.Bold;
            total.Inlines.Add(new Run($"TOTAL:                          {Total,8:F2}"));
            total.Inlines.Add(new LineBreak());
            total.Inlines.Add(new Run($"Cash Received:                  {Total,8:F2}"));
            total.Inlines.Add(new LineBreak());
            total.Inlines.Add(new Run($"Balance:                        {0.00,8:F2}"));
            
            // Savings
            decimal savings = Subtotal * 0.10m; // 10% savings calculation
            Paragraph savingsLine = new Paragraph();
            savingsLine.FontSize = 11;
            savingsLine.Inlines.Add(new Run($"You have saved Rs. {savings:F2}"));
            savingsLine.Inlines.Add(new LineBreak());
            savingsLine.Inlines.Add(new LineBreak());
            
            // Footer
            Paragraph footer = new Paragraph();
            footer.TextAlignment = TextAlignment.Center;
            footer.FontSize = 11;
            footer.FontWeight = FontWeights.Bold;
            footer.Inlines.Add(new Run("         THANK YOU..! VISIT AGAIN..!"));
            footer.Inlines.Add(new LineBreak());
            footer.Inlines.Add(new Run("RETURNS WILL BE ACCEPTED WITHIN 7 DAYS ONLY"));
            
            // Add all paragraphs to section
            section.Blocks.Add(header);
            section.Blocks.Add(address);
            section.Blocks.Add(invoiceHeader);
            section.Blocks.Add(billInfo);
            section.Blocks.Add(separator);
            section.Blocks.Add(itemHeader);
            section.Blocks.Add(totalSeparator);
            section.Blocks.Add(totalsInfo);
            section.Blocks.Add(total);
            section.Blocks.Add(savingsLine);
            section.Blocks.Add(footer);
            
            document.Blocks.Add(section);
            return document;
        }

        private string GenerateBillText()
        {
            var bill = new System.Text.StringBuilder();
            bill.AppendLine("                 AAPNA CHEMIST");
            bill.AppendLine("Hathi Bhai Patel Building, Shop No.1/2, Dist.Palghar");
            bill.AppendLine("Bhiwandi, Wada Road, Opp.Police Stn, Kudus-421312.");
            bill.AppendLine("GST NO: 27AEGPG3762F1ZM   Mob: 9890581131");
            bill.AppendLine();
            bill.AppendLine("INVOICE");
            bill.AppendLine($"Bill #: {BillNumber}    User: Admin     Date: {InvoiceDate}");
            bill.AppendLine($"Counter: POS  Time: {CurrentTime}");
            bill.AppendLine();
            bill.AppendLine("--------------------------------------------------");
            bill.AppendLine("Sr  Item Name        Qty  MRP   Rate   Amount");
            bill.AppendLine("--------------------------------------------------");

            foreach (var item in BillItems)
            {
                bill.AppendLine($"{item.Sr,2}   {item.Name,-15} {item.Quantity,3}  {item.Rate,4}  {item.Rate,5}  {item.Amount,7:F2}");
            }

            bill.AppendLine("--------------------------------------------------");
            
            int totalQty = BillItems.Sum(b => b.Quantity);
            int totalItems = BillItems.Count;
            decimal coinAdj = 0.00m;
            
            bill.AppendLine($"Tot Qty: {totalQty}   Tot Items: {totalItems}   Coin Adj: {coinAdj:F2}");
            bill.AppendLine();
            bill.AppendLine($"TOTAL:                           {Total,8:F2}");
            bill.AppendLine($"Cash Received:                   {Total,8:F2}");
            bill.AppendLine($"Balance:                         {0.00,8:F2}");
            bill.AppendLine();
            
            decimal savings = Subtotal * 0.10m;
            bill.AppendLine($"You have saved Rs. {savings:F2}");
            bill.AppendLine();
            bill.AppendLine("         THANK YOU..! VISIT AGAIN..!");
            bill.AppendLine("RETURNS WILL BE ACCEPTED WITHIN 7 DAYS ONLY");

            return bill.ToString();
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
                // Generate bill text for preview
                string billText = GenerateBillText();
                
                // Show the bill in a message box for preview
                MessageBox.Show(billText, "Bill Preview", MessageBoxButton.OK, MessageBoxImage.Information);
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
}