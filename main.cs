using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Windows.Forms;
using System.Text.Json;

namespace SupermarketCheckoutSystem
{
    // Modelos de dados
    public class Product
    {
        public string Name { get; set; }
        public decimal Price { get; set; }
        public int Quantity { get; set; }
        public decimal Subtotal => Price * Quantity;
    }

    public class Sale
    {
        public DateTime Date { get; } = DateTime.Now;
        public BindingList<Product> Products { get; } = new BindingList<Product>();
        public decimal Total => Products.Sum(p => p.Subtotal);
        public string SaleNumber { get; } = Guid.NewGuid().ToString().Substring(0, 8).ToUpper();
    }

    // Serviços
    public interface IMessageService
    {
        void ShowInformation(string message);
        void ShowWarning(string message);
        void ShowError(string message);
        bool Confirm(string question);
    }

    public class MessageBoxService : IMessageService
    {
        public void ShowInformation(string message) => 
            MessageBox.Show(message, "Information", MessageBoxButtons.OK, MessageBoxIcon.Information);

        public void ShowWarning(string message) => 
            MessageBox.Show(message, "Warning", MessageBoxButtons.OK, MessageBoxIcon.Warning);

        public void ShowError(string message) => 
            MessageBox.Show(message, "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);

        public bool Confirm(string question) => 
            MessageBox.Show(question, "Confirm", MessageBoxButtons.YesNo, MessageBoxIcon.Question) == DialogResult.Yes;
    }

    // Formulário principal
    public class CheckoutForm : Form
    {
        private readonly List<Sale> _salesHistory = new List<Sale>();
        private readonly IMessageService _messageService = new MessageBoxService();
        
        private Sale CurrentSale => _salesHistory.Last();
        
        // Controles UI
        private TextBox _nameTextBox;
        private TextBox _priceTextBox;
        private TextBox _quantityTextBox;
        private DataGridView _productsGridView;
        private StatusStrip _statusStrip;
        private ToolStripStatusLabel _totalLabel;
        private ToolStripStatusLabel _itemsLabel;
        private ToolStripStatusLabel _saleNumberLabel;

        public CheckoutForm()
        {
            InitializeComponents();
            InitializeNewSale();
            LoadSalesHistory();
        }

        private void InitializeComponents()
        {
            // Configuração básica do formulário
            Text = "Supermarket Checkout System";
            Size = new System.Drawing.Size(800, 600);
            StartPosition = FormStartPosition.CenterScreen;
            Font = new System.Drawing.Font("Segoe UI", 10F);
            
            // Layout principal
            var mainTable = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                ColumnCount = 1,
                RowCount = 4,
                Padding = new Padding(10)
            };
            
            // Painel de entrada
            var inputPanel = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                ColumnCount = 3,
                RowCount = 3
            };
            
            _nameTextBox = new TextBox { PlaceholderText = "Product name", Dock = DockStyle.Fill };
            _priceTextBox = new TextBox { PlaceholderText = "Price", Dock = DockStyle.Fill };
            _quantityTextBox = new TextBox { PlaceholderText = "Quantity", Dock = DockStyle.Fill };
            
            inputPanel.Controls.Add(new Label { Text = "Name:", TextAlign = System.Drawing.ContentAlignment.MiddleRight }, 0, 0);
            inputPanel.Controls.Add(_nameTextBox, 1, 0);
            inputPanel.Controls.Add(new Label { Text = "Price:", TextAlign = System.Drawing.ContentAlignment.MiddleRight }, 0, 1);
            inputPanel.Controls.Add(_priceTextBox, 1, 1);
            inputPanel.Controls.Add(new Label { Text = "Quantity:", TextAlign = System.Drawing.ContentAlignment.MiddleRight }, 0, 2);
            inputPanel.Controls.Add(_quantityTextBox, 1, 2);
            
            var addButton = new Button { Text = "&Add Product", Dock = DockStyle.Fill };
            addButton.Click += AddProduct_Click;
            inputPanel.Controls.Add(addButton, 2, 0);
            
            var finishButton = new Button { Text = "&Finish Sale", Dock = DockStyle.Fill };
            finishButton.Click += FinishSale_Click;
            inputPanel.Controls.Add(finishButton, 2, 1);
            
            var newSaleButton = new Button { Text = "&New Sale", Dock = DockStyle.Fill };
            newSaleButton.Click += NewSale_Click;
            inputPanel.Controls.Add(newSaleButton, 2, 2);
            
            // Grade de produtos
            _productsGridView = new DataGridView
            {
                Dock = DockStyle.Fill,
                ReadOnly = true,
                AutoGenerateColumns = false,
                AllowUserToAddRows = false,
                AllowUserToDeleteRows = false,
                SelectionMode = DataGridViewSelectionMode.FullRowSelect
            };
            
            _productsGridView.Columns.AddRange(
                new DataGridViewTextBoxColumn { HeaderText = "Name", DataPropertyName = "Name", Width = 200 },
                new DataGridViewTextBoxColumn { HeaderText = "Price", DataPropertyName = "Price", DefaultCellStyle = new DataGridViewCellStyle { Format = "C2" } },
                new DataGridViewTextBoxColumn { HeaderText = "Qty", DataPropertyName = "Quantity" },
                new DataGridViewTextBoxColumn { HeaderText = "Subtotal", DataPropertyName = "Subtotal", DefaultCellStyle = new DataGridViewCellStyle { Format = "C2" } }
            );
            
            // Barra de status
            _statusStrip = new StatusStrip();
            _saleNumberLabel = new ToolStripStatusLabel { Text = "Sale: #", Font = new System.Drawing.Font("Segoe UI", 9F, System.Drawing.FontStyle.Bold) };
            _itemsLabel = new ToolStripStatusLabel { Text = "Items: 0" };
            _totalLabel = new ToolStripStatusLabel { Text = "Total: $0.00", Font = new System.Drawing.Font("Segoe UI", 9F, System.Drawing.FontStyle.Bold) };
            
            _statusStrip.Items.AddRange(new ToolStripItem[] { _saleNumberLabel, _itemsLabel, new ToolStripStatusLabel { Spring = true }, _totalLabel });
            
            // Montar interface
            mainTable.Controls.Add(inputPanel, 0, 0);
            mainTable.Controls.Add(_productsGridView, 0, 1);
            mainTable.Controls.Add(_statusStrip, 0, 2);
            
            Controls.Add(mainTable);
            
            // Configurar atalhos
            KeyPreview = true;
            KeyDown += (s, e) =>
            {
                if (e.KeyCode == Keys.Enter) AddProduct_Click(s, e);
                if (e.KeyCode == Keys.F2) FinishSale_Click(s, e);
                if (e.KeyCode == Keys.F5) NewSale_Click(s, e);
            };
        }
        
        private void InitializeNewSale()
        {
            _salesHistory.Add(new Sale());
            _productsGridView.DataSource = CurrentSale.Products;
            CurrentSale.Products.ListChanged += (s, e) => UpdateStatusBar();
            UpdateStatusBar();
            ClearInputs();
            _nameTextBox.Focus();
        }
        
        private void UpdateStatusBar()
        {
            _saleNumberLabel.Text = $"Sale: #{CurrentSale.SaleNumber}";
            _itemsLabel.Text = $"Items: {CurrentSale.Products.Count}";
            _totalLabel.Text = $"Total: {CurrentSale.Total:C2}";
        }
        
        private void ClearInputs()
        {
            _nameTextBox.Clear();
            _priceTextBox.Clear();
            _quantityTextBox.Clear();
        }
        
        private void AddProduct_Click(object sender, EventArgs e)
        {
            try
            {
                var product = ValidateAndCreateProduct();
                CurrentSale.Products.Add(product);
                ClearInputs();
                _nameTextBox.Focus();
                LogAction($"Added product: {product.Name}");
            }
            catch (ArgumentException ex)
            {
                _messageService.ShowError(ex.Message);
                if (ex.Message.Contains("Price")) _priceTextBox.Focus();
                else if (ex.Message.Contains("Quantity")) _quantityTextBox.Focus();
                else _nameTextBox.Focus();
            }
        }
        
        private Product ValidateAndCreateProduct()
        {
            // Validação do nome
            var name = _nameTextBox.Text.Trim();
            if (string.IsNullOrWhiteSpace(name))
                throw new ArgumentException("Product name is required");
                
            if (name.Length > 50)
                throw new ArgumentException("Product name is too long (max 50 chars)");
                
            // Validação do preço
            if (!decimal.TryParse(_priceTextBox.Text, NumberStyles.Currency, CultureInfo.CurrentCulture, out decimal price) || price <= 0)
                throw new ArgumentException("Invalid price value");
                
            // Validação da quantidade
            if (!int.TryParse(_quantityTextBox.Text, out int quantity) || quantity <= 0)
                throw new ArgumentException("Invalid quantity value");
                
            return new Product
            {
                Name = name,
                Price = decimal.Round(price, 2),
                Quantity = quantity
            };
        }
        
        private void FinishSale_Click(object sender, EventArgs e)
        {
            if (CurrentSale.Products.Count == 0)
            {
                _messageService.ShowWarning("No products in current sale");
                return;
            }
            
            var message = $"Finish sale #{CurrentSale.SaleNumber}?\n\nTotal: {CurrentSale.Total:C2}";
            if (_messageService.Confirm(message))
            {
                _messageService.ShowInformation($"Sale #{CurrentSale.SaleNumber} completed\nTotal: {CurrentSale.Total:C2}");
                SaveSalesHistory();
                InitializeNewSale();
                LogAction($"Completed sale #{CurrentSale.SaleNumber}");
            }
        }
        
        private void NewSale_Click(object sender, EventArgs e)
        {
            if (CurrentSale.Products.Count > 0)
            {
                if (!_messageService.Confirm("Current sale has items. Start new sale anyway?"))
                    return;
            }
            
            InitializeNewSale();
            LogAction("Started new sale");
        }
        
        private void LoadSalesHistory()
        {
            try
            {
                if (File.Exists("sales_history.json"))
                {
                    var json = File.ReadAllText("sales_history.json");
                    _salesHistory = JsonSerializer.Deserialize<List<Sale>>(json) ?? new List<Sale>();
                }
            }
            catch (Exception ex)
            {
                LogError($"Failed to load sales history: {ex.Message}");
            }
        }
        
        private void SaveSalesHistory()
        {
            try
            {
                var options = new JsonSerializerOptions { WriteIndented = true };
                var json = JsonSerializer.Serialize(_salesHistory, options);
                File.WriteAllText("sales_history.json", json);
            }
            catch (Exception ex)
            {
                LogError($"Failed to save sales history: {ex.Message}");
            }
        }
        
        private void LogAction(string message)
        {
            File.AppendAllText("activity_log.txt", $"[{DateTime.Now}] {message}\n");
        }
        
        private void LogError(string error)
        {
            File.AppendAllText("error_log.txt", $"[{DateTime.Now}] {error}\n");
        }
        
        [STAThread]
        public static void Main()
        {
            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);
            Application.Run(new CheckoutForm());
        }
    }
}
