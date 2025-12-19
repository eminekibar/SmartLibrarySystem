using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;
using SmartLibrarySystem.BLL;
using SmartLibrarySystem.Models;

namespace SmartLibrarySystem.UI
{
    public class AdminDashboard : Form
    {
        private readonly User currentUser;
        private readonly BookService bookService = new BookService();
        private readonly UserService userService = new UserService();
        private readonly RequestService requestService = new RequestService();
        private AutoCompleteStringCollection authorAutoComplete;
        private AutoCompleteStringCollection categoryAutoComplete;

        private DataGridView booksGrid;
        private TextBox txtTitle;
        private TextBox txtAuthor;
        private TextBox txtCategory;
        private NumericUpDown numYear;
        private NumericUpDown numStock;
        private TextBox txtShelf;
        private TextBox txtSearchBookTitle;
        private TextBox txtSearchBookAuthor;
        private TextBox txtSearchBookCategory;
        private NumericUpDown numSearchYear;

        private DataGridView usersGrid;
        private TextBox txtUserName;
        private TextBox txtUserEmail;
        private TextBox txtUserSchool;
        private TextBox txtUserPhone;
        private TextBox txtUserPassword;
        private ComboBox cmbUserRole;
        private Button btnUpdateUser;
        private TextBox txtSearchEmail;
        private TextBox txtSearchFirstName;
        private TextBox txtSearchLastName;
        private TextBox txtSearchSchool;

        private DataGridView topBooksGrid;
        private Label lblDaily;
        private Label lblWeekly;
        private Label lblMonthly;
        private Button logoutButton;
        private TabPage reportsTab;
        private Panel statsCanvas;
        private Panel trendCanvas;
        private FlowLayoutPanel pieSwitchPanel;
        private Button pieBtnToday;
        private Button pieBtnWeek;
        private Button pieBtnMonth;
        private int dailyCount;
        private int weeklyCount;
        private int monthlyCount;
        private int statusPending;
        private int statusApproved;
        private int statusDelivered;
        private int statusReturned;
        private double weeklyChangePercent;
        private double monthlyChangePercent;
        private Label cardDailyValue;
        private Label cardWeeklyValue;
        private Label cardMonthlyValue;
        private Label cardWeeklyTrend;
        private Label cardMonthlyTrend;
        private Label statusLabel;
        private int loadingDepth;
        private DateTime loadingStartedAt;
        private Timer loadingTimer;
        private const string DateTimeDisplayFormat = "dd.MM.yyyy HH:mm";
        private readonly Color roleColor = Color.FromArgb(246, 153, 63);
        private readonly List<KeyValuePair<DateTime, int>> trendSeries = new List<KeyValuePair<DateTime, int>>();
        private List<BorrowRequest> allRequests = new List<BorrowRequest>();
        private string pieRangeKey = "30";

        public AdminDashboard(User user)
        {
            currentUser = user;
            InitializeComponent();
            LoadBooks();
            LoadUsers();
            LoadReports();
            SetupAutoCompleteSources();
        }

        private void InitializeComponent()
        {
            Text = $"Yönetici Paneli - {currentUser.FullName}";
            WindowState = FormWindowState.Normal;
            StartPosition = FormStartPosition.CenterScreen;
            Size = new Size(1200, 800);

            var tabs = new TabControl { Dock = DockStyle.Fill };
            tabs.TabPages.Add(CreateBooksTab());
            tabs.TabPages.Add(CreateUsersTab());
            reportsTab = CreateReportsTab();
            tabs.TabPages.Add(reportsTab);
            tabs.Selected += (_, __) =>
            {
                if (tabs.SelectedTab == reportsTab)
                {
                    LoadReports();
                }
            };

            logoutButton = new Button
            {
                Text = "🚪 Çıkış Yap",
                AutoSize = true,
                Anchor = AnchorStyles.Top | AnchorStyles.Right
            };
            logoutButton.Click += (_, __) => Logout();

            statusLabel = new Label
            {
                Text = "İşleniyor...",
                AutoSize = true,
                ForeColor = Color.DimGray,
                Visible = false,
                Anchor = AnchorStyles.Bottom | AnchorStyles.Right
            };

            var container = new Panel { Dock = DockStyle.Fill };
            container.Controls.Add(tabs);
            container.Controls.Add(logoutButton);
            container.Controls.Add(statusLabel);
            logoutButton.BringToFront();
            statusLabel.BringToFront();
            container.Resize += (_, __) => PositionLogoutButton(container);
            container.Resize += (_, __) => PositionStatusLabel(container);
            PositionLogoutButton(container);
            PositionStatusLabel(container);

            Controls.Add(container);
        }

        private TabPage CreateBooksTab()
        {
            var tab = new TabPage("Kitap Yönetimi");

            var layout = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                RowCount = 2,
                ColumnCount = 2
            };
            layout.RowStyles.Add(new RowStyle(SizeType.Percent, 70));
            layout.RowStyles.Add(new RowStyle(SizeType.Percent, 30));
            layout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 70));
            layout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 30));

            booksGrid = new DataGridView
            {
                Dock = DockStyle.Fill,
                ReadOnly = true,
                AutoGenerateColumns = false,
                SelectionMode = DataGridViewSelectionMode.FullRowSelect,
                AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill,
                AllowUserToAddRows = false,
                AllowUserToDeleteRows = false,
                MultiSelect = false
            };
            booksGrid.Columns.Add(new DataGridViewTextBoxColumn { DataPropertyName = nameof(Book.BookId), HeaderText = "No", Width = 50 });
            booksGrid.Columns.Add(new DataGridViewTextBoxColumn { DataPropertyName = nameof(Book.Title), HeaderText = "Kitap Adı" });
            booksGrid.Columns.Add(new DataGridViewTextBoxColumn { DataPropertyName = nameof(Book.Author), HeaderText = "Yazar" });
            booksGrid.Columns.Add(new DataGridViewTextBoxColumn { DataPropertyName = nameof(Book.Category), HeaderText = "Kategori" });
            booksGrid.Columns.Add(new DataGridViewTextBoxColumn { DataPropertyName = nameof(Book.PublishYear), HeaderText = "Yıl", Width = 60 });
            booksGrid.Columns.Add(new DataGridViewTextBoxColumn { DataPropertyName = nameof(Book.Stock), HeaderText = "Stok", Width = 60 });
            booksGrid.Columns.Add(new DataGridViewTextBoxColumn { DataPropertyName = nameof(Book.Shelf), HeaderText = "Raf", Width = 80 });
            booksGrid.SelectionChanged += (_, __) => FillBookForm();

            layout.Controls.Add(booksGrid, 0, 0);
            layout.SetRowSpan(booksGrid, 2);
            layout.Controls.Add(CreateBookFormPanel(), 1, 0);
            layout.Controls.Add(CreateBookFilterPanel(), 1, 1);

            tab.Controls.Add(layout);
            return tab;
        }

        private Control CreateBookFormPanel()
        {
            var panel = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                ColumnCount = 2,
                RowCount = 7,
                Padding = new Padding(10)
            };
            panel.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));
            panel.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
            for (int i = 0; i < 7; i++)
            {
                panel.RowStyles.Add(new RowStyle(SizeType.AutoSize));
            }

            txtTitle = new TextBox { Dock = DockStyle.Fill };
            txtAuthor = new TextBox { Dock = DockStyle.Fill };
            txtCategory = new TextBox { Dock = DockStyle.Fill };
            var currentYear = DateTime.Now.Year + 1;
            numYear = new NumericUpDown { Minimum = 1900, Maximum = currentYear, Value = Math.Min(2024, currentYear), Dock = DockStyle.Fill };
            numStock = new NumericUpDown { Minimum = 1, Maximum = 1000, Value = 1, Dock = DockStyle.Fill };
            txtShelf = new TextBox { Dock = DockStyle.Fill };

            panel.Controls.Add(new Label { Text = "Başlık", AutoSize = true }, 0, 0);
            panel.Controls.Add(txtTitle, 1, 0);
            panel.Controls.Add(new Label { Text = "Yazar", AutoSize = true }, 0, 1);
            panel.Controls.Add(txtAuthor, 1, 1);
            panel.Controls.Add(new Label { Text = "Kategori", AutoSize = true }, 0, 2);
            panel.Controls.Add(txtCategory, 1, 2);
            panel.Controls.Add(new Label { Text = "Yıl", AutoSize = true }, 0, 3);
            panel.Controls.Add(numYear, 1, 3);
            panel.Controls.Add(new Label { Text = "Stok", AutoSize = true }, 0, 4);
            panel.Controls.Add(numStock, 1, 4);
            panel.Controls.Add(new Label { Text = "Raf Konumu", AutoSize = true }, 0, 5);
            panel.Controls.Add(txtShelf, 1, 5);

            var addButton = new Button { Text = "➕ Ekle", Dock = DockStyle.Fill };
            addButton.Click += (_, __) => AddBook();
            var updateButton = new Button { Text = "✏️ Güncelle", Dock = DockStyle.Fill };
            updateButton.Click += (_, __) => UpdateBook();
            var deleteButton = new Button { Text = "🗑️ Sil", Dock = DockStyle.Fill };
            deleteButton.Click += (_, __) => DeleteBook();
            var clearButton = new Button { Text = "🧹 Temizle", Dock = DockStyle.Fill };
            clearButton.Click += (_, __) =>
            {
                ClearBookForm();
                booksGrid.ClearSelection();
            };
            var cancelButton = new Button { Text = "❌ İptal", Dock = DockStyle.Fill };
            cancelButton.Click += (_, __) =>
            {
                ClearBookForm();
                booksGrid.ClearSelection();
            };
            StylePrimaryButton(addButton);
            StylePrimaryButton(updateButton);
            StylePrimaryButton(deleteButton);
            StyleSecondaryButton(clearButton);
            StyleSecondaryButton(cancelButton);

            var buttonsPanel = new TableLayoutPanel
            {
                Dock = DockStyle.Top,
                ColumnCount = 1,
                RowCount = 5,
                AutoSize = true,
                AutoSizeMode = AutoSizeMode.GrowAndShrink
            };
            buttonsPanel.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
            for (int i = 0; i < 5; i++)
            {
                buttonsPanel.RowStyles.Add(new RowStyle(SizeType.AutoSize));
            }

            buttonsPanel.Controls.Add(addButton, 0, 0);
            buttonsPanel.Controls.Add(updateButton, 0, 1);
            buttonsPanel.Controls.Add(deleteButton, 0, 2);
            buttonsPanel.Controls.Add(clearButton, 0, 3);
            buttonsPanel.Controls.Add(cancelButton, 0, 4);

            panel.Controls.Add(buttonsPanel, 0, 6);
            panel.SetColumnSpan(buttonsPanel, 2);

            return panel;
        }

        private Control CreateBookFilterPanel()
        {
            var panel = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                ColumnCount = 2,
                RowCount = 5,
                Padding = new Padding(10)
            };
            panel.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));
            panel.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
            for (int i = 0; i < 5; i++)
            {
                panel.RowStyles.Add(new RowStyle(SizeType.AutoSize));
            }

            txtSearchBookTitle = new TextBox { Dock = DockStyle.Fill, PlaceholderText = "Başlık" };
            txtSearchBookAuthor = new TextBox { Dock = DockStyle.Fill, PlaceholderText = "Yazar" };
            txtSearchBookCategory = new TextBox { Dock = DockStyle.Fill, PlaceholderText = "Kategori" };
            numSearchYear = new NumericUpDown { Dock = DockStyle.Left, Minimum = 0, Maximum = 2100, Width = 120 };

            panel.Controls.Add(new Label { Text = "Başlık", AutoSize = true }, 0, 0);
            panel.Controls.Add(txtSearchBookTitle, 1, 0);
            panel.Controls.Add(new Label { Text = "Yazar", AutoSize = true }, 0, 1);
            panel.Controls.Add(txtSearchBookAuthor, 1, 1);
            panel.Controls.Add(new Label { Text = "Kategori", AutoSize = true }, 0, 2);
            panel.Controls.Add(txtSearchBookCategory, 1, 2);
            panel.Controls.Add(new Label { Text = "Yıl", AutoSize = true }, 0, 3);
            panel.Controls.Add(numSearchYear, 1, 3);

            var buttons = new FlowLayoutPanel
            {
                Dock = DockStyle.Fill,
                FlowDirection = FlowDirection.LeftToRight,
                AutoSize = true,
                AutoSizeMode = AutoSizeMode.GrowAndShrink,
                Padding = new Padding(0, 6, 0, 0)
            };
            var btnApply = new Button { Text = "🔍 Ara", AutoSize = true };
            btnApply.Click += (_, __) => ApplyBookFilter();
            var btnClear = new Button { Text = "🧹 Filtreyi Temizle", AutoSize = true, Margin = new Padding(8, 0, 0, 0) };
            btnClear.Click += (_, __) => ClearBookFilter();
            StyleSecondaryButton(btnApply);
            StyleSecondaryButton(btnClear);
            buttons.Controls.Add(btnApply);
            buttons.Controls.Add(btnClear);

            panel.Controls.Add(buttons, 1, 4);

            return panel;
        }

        private TabPage CreateUsersTab()
        {
            var tab = new TabPage("Kullanıcı Yönetimi");
            var layout = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                RowCount = 1,
                ColumnCount = 2
            };
            layout.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
            layout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 70));
            layout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 30));

            usersGrid = new DataGridView
            {
                Dock = DockStyle.Fill,
                ReadOnly = true,
                AutoGenerateColumns = false,
                SelectionMode = DataGridViewSelectionMode.FullRowSelect,
                AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill,
                AllowUserToAddRows = false,
                AllowUserToDeleteRows = false,
                MultiSelect = false
            };
            usersGrid.Columns.Add(new DataGridViewTextBoxColumn { DataPropertyName = nameof(User.UserId), HeaderText = "No", Width = 50 });
            usersGrid.Columns.Add(new DataGridViewTextBoxColumn { DataPropertyName = nameof(User.FullName), HeaderText = "Ad Soyad" });
            usersGrid.Columns.Add(new DataGridViewTextBoxColumn { DataPropertyName = nameof(User.Email), HeaderText = "E-posta" });
            usersGrid.Columns.Add(new DataGridViewTextBoxColumn { DataPropertyName = nameof(User.SchoolNumber), HeaderText = "Okul No", Width = 90 });
            usersGrid.Columns.Add(new DataGridViewTextBoxColumn { DataPropertyName = nameof(User.Phone), HeaderText = "Telefon", Width = 110 });
            usersGrid.Columns.Add(new DataGridViewTextBoxColumn { DataPropertyName = nameof(User.Role), HeaderText = "Rol", Width = 90 });
            usersGrid.Columns.Add(new DataGridViewTextBoxColumn { DataPropertyName = nameof(User.CreatedAt), HeaderText = "Kayıt Tarihi", Width = 130 });
            usersGrid.SelectionChanged += (_, __) => FillUserForm();
            usersGrid.CellFormatting += UsersGrid_CellFormatting;

            var rightPanel = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                RowCount = 2,
                ColumnCount = 1
            };
            rightPanel.RowStyles.Add(new RowStyle(SizeType.Percent, 70));
            rightPanel.RowStyles.Add(new RowStyle(SizeType.Percent, 30));
            rightPanel.Controls.Add(CreateUserFormPanel(), 0, 0);
            rightPanel.Controls.Add(CreateUserFilterPanel(), 0, 1);

            layout.Controls.Add(usersGrid, 0, 0);
            layout.Controls.Add(rightPanel, 1, 0);
            tab.Controls.Add(layout);
            return tab;
        }

        private Control CreateUserFormPanel()
        {
            var panel = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                ColumnCount = 2,
                RowCount = 7,
                Padding = new Padding(10)
            };
            panel.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));
            panel.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
            for (int i = 0; i < 7; i++)
            {
                panel.RowStyles.Add(new RowStyle(SizeType.AutoSize));
            }

            txtUserName = new TextBox { Dock = DockStyle.Fill };
            txtUserEmail = new TextBox { Dock = DockStyle.Fill };
            txtUserSchool = new TextBox { Dock = DockStyle.Fill };
            txtUserPhone = new TextBox { Dock = DockStyle.Fill };
            txtUserPhone.KeyPress += DigitOnly_KeyPress;
            txtUserPassword = new TextBox { PasswordChar = '*', Dock = DockStyle.Fill };
            cmbUserRole = new ComboBox { DropDownStyle = ComboBoxStyle.DropDownList, Dock = DockStyle.Fill };
            cmbUserRole.DisplayMember = "Value";
            cmbUserRole.ValueMember = "Key";
            foreach (var pair in RoleConstants.DisplayNames)
            {
                cmbUserRole.Items.Add(new KeyValuePair<string, string>(pair.Key, pair.Value));
            }
            cmbUserRole.SelectedIndex = 0;

            panel.Controls.Add(new Label { Text = "Ad Soyad", AutoSize = true }, 0, 0);
            panel.Controls.Add(txtUserName, 1, 0);
            panel.Controls.Add(new Label { Text = "E-posta", AutoSize = true }, 0, 1);
            panel.Controls.Add(txtUserEmail, 1, 1);
            panel.Controls.Add(new Label { Text = "Okul No", AutoSize = true }, 0, 2);
            panel.Controls.Add(txtUserSchool, 1, 2);
            panel.Controls.Add(new Label { Text = "Telefon", AutoSize = true }, 0, 3);
            panel.Controls.Add(txtUserPhone, 1, 3);
            panel.Controls.Add(new Label { Text = "Şifre", AutoSize = true }, 0, 4);
            panel.Controls.Add(txtUserPassword, 1, 4);
            panel.Controls.Add(new Label { Text = "Rol", AutoSize = true }, 0, 5);
            panel.Controls.Add(cmbUserRole, 1, 5);

            var addButton = new Button { Text = "➕ Kullanıcı Ekle", Dock = DockStyle.Fill };
            addButton.Click += (_, __) => AddUser();
            var deleteButton = new Button { Text = "🗑️ Sil", Dock = DockStyle.Fill };
            deleteButton.Click += (_, __) => DeleteUser();
            btnUpdateUser = new Button { Text = "✏️ Seçileni Güncelle", Dock = DockStyle.Fill };
            btnUpdateUser.Click += (_, __) => UpdateSelectedUser();
            var refreshButton = new Button { Text = "♻️ Yenile", Dock = DockStyle.Fill };
            refreshButton.Click += (_, __) => LoadUsers();
            var cancelButton = new Button { Text = "❌ İptal", Dock = DockStyle.Fill };
            cancelButton.Click += (_, __) =>
            {
                usersGrid.ClearSelection();
                txtUserName.Clear();
                txtUserEmail.Clear();
                txtUserSchool.Clear();
                txtUserPhone.Clear();
                txtUserPassword.Clear();
                cmbUserRole.SelectedIndex = 0;
            };
            StylePrimaryButton(addButton);
            StylePrimaryButton(btnUpdateUser);
            StylePrimaryButton(deleteButton);
            StyleSecondaryButton(refreshButton);
            StyleSecondaryButton(cancelButton);

            var buttonsPanel = new TableLayoutPanel
            {
                Dock = DockStyle.Top,
                ColumnCount = 1,
                RowCount = 5,
                AutoSize = true,
                AutoSizeMode = AutoSizeMode.GrowAndShrink
            };
            buttonsPanel.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
            for (int i = 0; i < 5; i++)
            {
                buttonsPanel.RowStyles.Add(new RowStyle(SizeType.AutoSize));
            }

            buttonsPanel.Controls.Add(addButton, 0, 0);
            buttonsPanel.Controls.Add(deleteButton, 0, 1);
            buttonsPanel.Controls.Add(btnUpdateUser, 0, 2);
            buttonsPanel.Controls.Add(refreshButton, 0, 3);
            buttonsPanel.Controls.Add(cancelButton, 0, 4);

            panel.Controls.Add(buttonsPanel, 0, 6);
            panel.SetColumnSpan(buttonsPanel, 2);

            return panel;
        }

        private Control CreateUserFilterPanel()
        {
            var panel = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                ColumnCount = 2,
                RowCount = 5,
                Padding = new Padding(10, 10, 10, 10)
            };
            panel.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));
            panel.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
            for (int i = 0; i < 5; i++)
            {
                panel.RowStyles.Add(new RowStyle(SizeType.AutoSize));
            }

            txtSearchFirstName = new TextBox { Dock = DockStyle.Fill, PlaceholderText = "Ad" };
            txtSearchLastName = new TextBox { Dock = DockStyle.Fill, PlaceholderText = "Soyad" };
            txtSearchEmail = new TextBox { Dock = DockStyle.Fill, PlaceholderText = "E-posta" };
            txtSearchSchool = new TextBox { Dock = DockStyle.Fill, PlaceholderText = "Okul No" };

            panel.Controls.Add(new Label { Text = "Ad", AutoSize = true }, 0, 0);
            panel.Controls.Add(txtSearchFirstName, 1, 0);
            panel.Controls.Add(new Label { Text = "Soyad", AutoSize = true }, 0, 1);
            panel.Controls.Add(txtSearchLastName, 1, 1);
            panel.Controls.Add(new Label { Text = "E-posta", AutoSize = true }, 0, 2);
            panel.Controls.Add(txtSearchEmail, 1, 2);
            panel.Controls.Add(new Label { Text = "Okul No", AutoSize = true }, 0, 3);
            panel.Controls.Add(txtSearchSchool, 1, 3);

            var buttons = new FlowLayoutPanel
            {
                Dock = DockStyle.Fill,
                FlowDirection = FlowDirection.LeftToRight,
                AutoSize = true,
                AutoSizeMode = AutoSizeMode.GrowAndShrink,
                Padding = new Padding(0, 6, 0, 0)
            };
            var btnApply = new Button { Text = "🔍 Ara", AutoSize = true };
            btnApply.Click += (_, __) => ApplyUserFilter();
            var btnClear = new Button { Text = "🧹 Filtreyi Temizle", AutoSize = true, Margin = new Padding(8, 0, 0, 0) };
            btnClear.Click += (_, __) => ClearUserFilter();
            StyleSecondaryButton(btnApply);
            StyleSecondaryButton(btnClear);
            buttons.Controls.Add(btnApply);
            buttons.Controls.Add(btnClear);

            panel.Controls.Add(buttons, 1, 4);

            return panel;
        }

        private TabPage CreateReportsTab()
        {
            var tab = new TabPage("Raporlar") { AutoScroll = true };

            // Ana dikey yerleşim: üstte buton, altta içerik
            var mainLayout = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                RowCount = 2,
                ColumnCount = 1,
                Padding = new Padding(20, 10, 20, 20)
            };
            mainLayout.RowStyles.Add(new RowStyle(SizeType.Percent, 18));
            mainLayout.RowStyles.Add(new RowStyle(SizeType.Percent, 82));

            // Alt kısım: istatistik kartları + yenile
            var footerCards = new FlowLayoutPanel
            {
                Dock = DockStyle.Fill,
                FlowDirection = FlowDirection.LeftToRight,
                AutoSize = true,
                AutoSizeMode = AutoSizeMode.GrowAndShrink,
                Padding = new Padding(0, 4, 0, 0)
            };

            Panel StatCard(string title, string description, Func<int> getter, Color color)
            {
                var panel = new Panel
                {
                    Width = 220,
                    Height = 90,
                    Margin = new Padding(0, 0, 12, 0),
                    BackColor = Color.WhiteSmoke
                };
                var titleLabel = new Label
                {
                    Text = title,
                    Font = new Font("Segoe UI", 9, FontStyle.Bold),
                    ForeColor = Color.FromArgb(55, 71, 79),
                    AutoSize = true,
                    Location = new Point(12, 10)
                };
                var descLabel = new Label
                {
                    Text = description,
                    Font = new Font("Segoe UI", 8, FontStyle.Regular),
                    ForeColor = Color.DimGray,
                    AutoSize = true,
                    Location = new Point(12, 28)
                };
                var valueLabel = new Label
                {
                    Text = getter().ToString(),
                    Font = new Font("Segoe UI", 18, FontStyle.Bold),
                    ForeColor = color,
                    AutoSize = true,
                    Location = new Point(12, 50)
                };
                panel.Controls.Add(titleLabel);
                panel.Controls.Add(descLabel);
                panel.Controls.Add(valueLabel);
                panel.Tag = valueLabel; // yeniden güncelleme için
                return panel;
            }

            var cardDaily = StatCard("Günlük", "Bugün oluşturulan ödünç talepleri", () => dailyCount, Color.FromArgb(90, 155, 212));
            cardDailyValue = (Label)cardDaily.Tag;
            var cardWeekly = StatCard("Haftalık", "Son 7 günde oluşturulan talepler", () => weeklyCount, Color.FromArgb(155, 200, 100));
            cardWeeklyValue = (Label)cardWeekly.Tag;
            var cardMonthly = StatCard("Aylık", "Son 30 günde oluşturulan talepler", () => monthlyCount, Color.FromArgb(246, 153, 63));
            cardMonthlyValue = (Label)cardMonthly.Tag;

            cardWeeklyTrend = new Label
            {
                Text = string.Empty,
                Font = new Font("Segoe UI", 8, FontStyle.Bold),
                ForeColor = Color.Gray,
                AutoSize = true,
                Location = new Point(12, 50)
            };
            cardMonthlyTrend = new Label
            {
                Text = string.Empty,
                Font = new Font("Segoe UI", 8, FontStyle.Bold),
                ForeColor = Color.Gray,
                AutoSize = true,
                Location = new Point(12, 50)
            };
            cardWeekly.Controls.Add(cardWeeklyTrend);
            cardMonthly.Controls.Add(cardMonthlyTrend);

            var refreshButton = new Button { Text = "Raporları Yenile", AutoSize = true, Margin = new Padding(8, 12, 0, 0) };
            refreshButton.Click += (_, __) => LoadReports();

            footerCards.Controls.Add(cardDaily);
            footerCards.Controls.Add(cardWeekly);
            footerCards.Controls.Add(cardMonthly);
            footerCards.Controls.Add(refreshButton);

            // İçerik: sol istatistikler, sağ top kitaplar
            var content = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                ColumnCount = 2,
                RowCount = 1,
                Margin = new Padding(0, 10, 0, 10)
            };
            content.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50));
            content.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50));
            content.RowStyles.Add(new RowStyle(SizeType.Percent, 100));

            // Sol panel: durum dağılımı + trend
            var leftPanel = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                RowCount = 3,
                ColumnCount = 1
            };
            leftPanel.RowStyles.Add(new RowStyle(SizeType.Percent, 80));
            leftPanel.RowStyles.Add(new RowStyle(SizeType.Percent, 20));
            leftPanel.RowStyles.Add(new RowStyle(SizeType.AutoSize));
            leftPanel.Padding = new Padding(0, 10, 10, 0);

            var pieContainer = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                RowCount = 2,
                ColumnCount = 1
            };
            pieContainer.RowStyles.Add(new RowStyle(SizeType.AutoSize));
            pieContainer.RowStyles.Add(new RowStyle(SizeType.Percent, 100));

            pieSwitchPanel = new FlowLayoutPanel
            {
                Dock = DockStyle.Top,
                FlowDirection = FlowDirection.LeftToRight,
                AutoSize = true,
                AutoSizeMode = AutoSizeMode.GrowAndShrink,
                Padding = new Padding(8, 4, 0, 4)
            };

            Button CreatePieButton(string text, string key)
            {
                var btn = new Button
                {
                    Text = text,
                    AutoSize = true,
                    Tag = key,
                    FlatStyle = FlatStyle.Standard,
                    Margin = new Padding(0, 0, 6, 0)
                };
                btn.Click += PieRangeButton_Click;
                return btn;
            }

            pieBtnToday = CreatePieButton("Bugün", "1");
            pieBtnWeek = CreatePieButton("7 Gün", "7");
            pieBtnMonth = CreatePieButton("30 Gün", "30");
            pieSwitchPanel.Controls.Add(pieBtnToday);
            pieSwitchPanel.Controls.Add(pieBtnWeek);
            pieSwitchPanel.Controls.Add(pieBtnMonth);

            statsCanvas = new Panel { Dock = DockStyle.Fill, BackColor = Color.WhiteSmoke, Margin = new Padding(0, 0, 0, 8) };
            statsCanvas.Paint += StatsCanvas_Paint;

            pieContainer.Controls.Add(pieSwitchPanel, 0, 0);
            pieContainer.Controls.Add(statsCanvas, 0, 1);

            trendCanvas = new Panel { Dock = DockStyle.Fill, BackColor = Color.WhiteSmoke, Margin = new Padding(0, 0, 0, 8), MinimumSize = new Size(0, 160) };
            trendCanvas.Paint += TrendCanvas_Paint;

            var statsPanel = new FlowLayoutPanel
            {
                Dock = DockStyle.Fill,
                FlowDirection = FlowDirection.TopDown,
                Padding = new Padding(10, 0, 0, 10)
            };
            lblDaily = new Label { Visible = false };
            lblWeekly = new Label { Visible = false };
            lblMonthly = new Label { Visible = false };

            leftPanel.Controls.Add(pieContainer, 0, 0);
            leftPanel.Controls.Add(trendCanvas, 0, 1);
            leftPanel.Controls.Add(statsPanel, 0, 2);

            // Sağ panel: başlık + grid
            var rightPanel = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                RowCount = 2,
                ColumnCount = 1
            };
            rightPanel.RowStyles.Add(new RowStyle(SizeType.AutoSize));
            rightPanel.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
            rightPanel.Padding = new Padding(10, 10, 0, 0);

            var topBooksLabel = new Label
            {
                Text = "En Çok Ödünç Alınan Kitaplar",
                AutoSize = true,
                Font = new Font("Segoe UI", 10, FontStyle.Bold),
                Padding = new Padding(0, 0, 0, 8)
            };

            topBooksGrid = new DataGridView
            {
                Dock = DockStyle.Fill,
                ReadOnly = true,
                AutoGenerateColumns = true,
                AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill,
                SelectionMode = DataGridViewSelectionMode.FullRowSelect
            };

            rightPanel.Controls.Add(topBooksLabel, 0, 0);
            rightPanel.Controls.Add(topBooksGrid, 0, 1);

            content.Controls.Add(leftPanel, 0, 0);
            content.Controls.Add(rightPanel, 1, 0);

            mainLayout.Controls.Add(footerCards, 0, 0);
            mainLayout.Controls.Add(content, 0, 1);

            tab.Controls.Add(mainLayout);
            return tab;
        }

        private void LoadBooks()
        {
            SetLoading(true);
            try
            {
                booksGrid.DataSource = new BindingSource { DataSource = new List<Book>(bookService.GetAll()) };
                SetupAutoCompleteSources();
            }
            finally
            {
                SetLoading(false);
            }
        }

        private void ApplyBookFilter()
        {
            var title = txtSearchBookTitle.Text.Trim();
            var author = txtSearchBookAuthor.Text.Trim();
            var category = txtSearchBookCategory.Text.Trim();
            var year = (int)numSearchYear.Value;

            var filtered = bookService.GetAll().Where(b =>
                (string.IsNullOrWhiteSpace(title) || b.Title?.IndexOf(title, StringComparison.OrdinalIgnoreCase) >= 0) &&
                (string.IsNullOrWhiteSpace(author) || b.Author?.IndexOf(author, StringComparison.OrdinalIgnoreCase) >= 0) &&
                (string.IsNullOrWhiteSpace(category) || b.Category?.IndexOf(category, StringComparison.OrdinalIgnoreCase) >= 0) &&
                (year == 0 || b.PublishYear == year)
            ).ToList();

            booksGrid.DataSource = new BindingSource { DataSource = filtered };
        }

        private void ClearBookFilter()
        {
            txtSearchBookTitle.Clear();
            txtSearchBookAuthor.Clear();
            txtSearchBookCategory.Clear();
            numSearchYear.Value = 0;
            LoadBooks();
        }

        private void FillBookForm()
        {
            if (booksGrid.CurrentRow?.DataBoundItem is Book book)
            {
                txtTitle.Text = book.Title;
                txtAuthor.Text = book.Author;
                txtCategory.Text = book.Category;
                numYear.Value = book.PublishYear;
                numStock.Value = book.Stock;
                txtShelf.Text = book.Shelf;
            }
        }

        private void AddBook()
        {
            var book = new Book
            {
                Title = txtTitle.Text.Trim(),
                Author = txtAuthor.Text.Trim(),
                Category = txtCategory.Text.Trim(),
                PublishYear = (int)numYear.Value,
                Stock = (int)numStock.Value,
                Shelf = txtShelf.Text.Trim()
            };

            var confirm = MessageBox.Show(
                $"\"{book.Title}\" kitabını eklemek istediğinize emin misiniz?",
                "Onay",
                MessageBoxButtons.YesNo,
                MessageBoxIcon.Question,
                MessageBoxDefaultButton.Button2);
            if (confirm != DialogResult.Yes)
            {
                return;
            }

            SetLoading(true);
            try
            {
                var validation = bookService.AddBook(book);
                if (!validation.IsValid)
                {
                    MessageBox.Show("Lütfen düzeltin:\n\n" + string.Join(Environment.NewLine, validation.Errors.Select(e => "• " + e)), "Hata", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    return;
                }

                LoadBooks();
                ClearBookForm();
                MessageBox.Show("Kitap eklendi.", "Bilgi", MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
            finally
            {
                SetLoading(false);
            }
        }

        private void UpdateBook()
        {
            if (!(booksGrid.CurrentRow?.DataBoundItem is Book selected))
            {
                MessageBox.Show("Lütfen güncellenecek kitabı seçin.", "Uyarı", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            selected.Title = txtTitle.Text.Trim();
            selected.Author = txtAuthor.Text.Trim();
            selected.Category = txtCategory.Text.Trim();
            selected.PublishYear = (int)numYear.Value;
            selected.Stock = (int)numStock.Value;
            selected.Shelf = txtShelf.Text.Trim();

            var confirm = MessageBox.Show(
                $"\"{selected.Title}\" kitabını güncellemek istediğinize emin misiniz?",
                "Onay",
                MessageBoxButtons.YesNo,
                MessageBoxIcon.Question,
                MessageBoxDefaultButton.Button2);
            if (confirm != DialogResult.Yes)
            {
                return;
            }

            SetLoading(true);
            try
            {
                var validation = bookService.UpdateBook(selected);
                if (!validation.IsValid)
                {
                    MessageBox.Show("Lütfen düzeltin:\n\n" + string.Join(Environment.NewLine, validation.Errors.Select(e => "• " + e)), "Hata", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    return;
                }

                LoadBooks();
                MessageBox.Show("Kitap güncellendi.", "Bilgi", MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
            finally
            {
                SetLoading(false);
            }
        }

        private void DeleteBook()
        {
            if (!(booksGrid.CurrentRow?.DataBoundItem is Book selected))
            {
                MessageBox.Show("Lütfen silinecek kitabı seçin.", "Uyarı", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            if (MessageBox.Show("Kitabı silmek istediğinize emin misiniz?", "Onay", MessageBoxButtons.YesNo, MessageBoxIcon.Question) == DialogResult.Yes)
            {
                SetLoading(true);
                try
                {
                    bookService.DeleteBook(selected.BookId);
                    LoadBooks();
                    ClearBookForm();
                    MessageBox.Show("Kitap silindi.", "Bilgi", MessageBoxButtons.OK, MessageBoxIcon.Information);
                }
                finally
                {
                    SetLoading(false);
                }
            }
        }

        private void ClearBookForm()
        {
            txtTitle.Clear();
            txtAuthor.Clear();
            txtCategory.Clear();
            txtShelf.Clear();
            numYear.Value = numYear.Minimum;
            numStock.Value = numStock.Minimum;
        }

        private void LoadUsers()
        {
            SetLoading(true);
            try
            {
                usersGrid.DataSource = new BindingSource { DataSource = new List<User>(userService.GetAll()) };
            }
            finally
            {
                SetLoading(false);
            }
        }

        private void DigitOnly_KeyPress(object sender, KeyPressEventArgs e)
        {
            if (!char.IsControl(e.KeyChar) && !char.IsDigit(e.KeyChar))
            {
                e.Handled = true;
            }
        }

        private void ApplyUserFilter()
        {
            var first = txtSearchFirstName.Text.Trim();
            var last = txtSearchLastName.Text.Trim();
            var email = txtSearchEmail.Text.Trim();
            var school = txtSearchSchool.Text.Trim();

            var filtered = userService.GetAll().Where(u =>
                (string.IsNullOrWhiteSpace(first) || (u.FullName?.IndexOf(first, StringComparison.OrdinalIgnoreCase) >= 0)) &&
                (string.IsNullOrWhiteSpace(last) || (u.FullName?.IndexOf(last, StringComparison.OrdinalIgnoreCase) >= 0)) &&
                (string.IsNullOrWhiteSpace(email) || (u.Email?.IndexOf(email, StringComparison.OrdinalIgnoreCase) >= 0)) &&
                (string.IsNullOrWhiteSpace(school) || (u.SchoolNumber?.IndexOf(school, StringComparison.OrdinalIgnoreCase) >= 0))
            ).ToList();

            usersGrid.DataSource = new BindingSource { DataSource = filtered };
        }

        private void UsersGrid_CellFormatting(object sender, DataGridViewCellFormattingEventArgs e)
        {
            var property = usersGrid.Columns[e.ColumnIndex].DataPropertyName;
            if (property == nameof(User.Role) && e.Value is string role)
            {
                e.Value = RoleConstants.ToDisplay(role);
                e.FormattingApplied = true;
                return;
            }

            if (property == nameof(User.CreatedAt) && e.Value is DateTime dt)
            {
                e.Value = dt == default ? string.Empty : dt.ToString(DateTimeDisplayFormat);
                e.FormattingApplied = true;
            }
        }

        private void ClearUserFilter()
        {
            txtSearchFirstName.Clear();
            txtSearchLastName.Clear();
            txtSearchEmail.Clear();
            txtSearchSchool.Clear();
            LoadUsers();
        }

        private void FillUserForm()
        {
            if (usersGrid.CurrentRow?.DataBoundItem is User user)
            {
                txtUserName.Text = user.FullName;
                txtUserEmail.Text = user.Email;
                txtUserSchool.Text = user.SchoolNumber;
                txtUserPhone.Text = user.Phone;
                SelectRoleValue(user.Role);
                txtUserPassword.Clear(); // güvenlik için parola doldurma
            }
        }

        private void SetupAutoCompleteSources()
        {
            var books = bookService.GetAll().ToList();
            authorAutoComplete = new AutoCompleteStringCollection();
            categoryAutoComplete = new AutoCompleteStringCollection();

            foreach (var author in books.Select(b => b.Author).Where(a => !string.IsNullOrWhiteSpace(a)).Distinct())
            {
                authorAutoComplete.Add(author);
            }
            foreach (var cat in books.Select(b => b.Category).Where(c => !string.IsNullOrWhiteSpace(c)).Distinct())
            {
                categoryAutoComplete.Add(cat);
            }

            void ApplyAutoComplete(TextBox box, AutoCompleteStringCollection source)
            {
                if (box == null || source == null) return;
                box.AutoCompleteMode = AutoCompleteMode.SuggestAppend;
                box.AutoCompleteSource = AutoCompleteSource.CustomSource;
                box.AutoCompleteCustomSource = source;
            }

            ApplyAutoComplete(txtAuthor, authorAutoComplete);
            ApplyAutoComplete(txtCategory, categoryAutoComplete);
            ApplyAutoComplete(txtSearchBookAuthor, authorAutoComplete);
            ApplyAutoComplete(txtSearchBookCategory, categoryAutoComplete);
        }

        private void SelectRoleValue(string role)
        {
            foreach (KeyValuePair<string, string> option in cmbUserRole.Items)
            {
                if (string.Equals(option.Key, role, StringComparison.OrdinalIgnoreCase))
                {
                    cmbUserRole.SelectedItem = option;
                    return;
                }
            }
        }

        private void AddUser()
        {
            var user = new User
            {
                FullName = txtUserName.Text.Trim(),
                Email = txtUserEmail.Text.Trim(),
                SchoolNumber = txtUserSchool.Text.Trim(),
                Phone = txtUserPhone.Text.Trim(),
                Role = cmbUserRole.SelectedValue?.ToString() ?? RoleConstants.Student
            };

            var confirm = MessageBox.Show(
                $"\"{user.FullName}\" kullanıcısını eklemek istediğinize emin misiniz?",
                "Onay",
                MessageBoxButtons.YesNo,
                MessageBoxIcon.Question,
                MessageBoxDefaultButton.Button2);
            if (confirm != DialogResult.Yes)
            {
                return;
            }

            SetLoading(true);
            try
            {
                var validation = userService.Register(user, txtUserPassword.Text);
                if (!validation.IsValid)
                {
                    MessageBox.Show("Lütfen düzeltin:\n\n" + string.Join(Environment.NewLine, validation.Errors.Select(e => "• " + e)), "Hata", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    return;
                }

                LoadUsers();
                MessageBox.Show("Kullanıcı eklendi.", "Bilgi", MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
            finally
            {
                SetLoading(false);
            }
        }

        private void UpdateSelectedUser()
        {
            if (!(usersGrid.CurrentRow?.DataBoundItem is User user))
            {
                MessageBox.Show("Lütfen güncellenecek kullanıcıyı seçin.", "Uyarı", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            user.FullName = txtUserName.Text.Trim();
            user.Email = txtUserEmail.Text.Trim();
            user.SchoolNumber = txtUserSchool.Text.Trim();
            user.Phone = txtUserPhone.Text.Trim();
            user.Role = cmbUserRole.SelectedValue?.ToString();

            var password = string.IsNullOrWhiteSpace(txtUserPassword.Text) ? null : txtUserPassword.Text;

            var confirm = MessageBox.Show(
                $"\"{user.FullName}\" kullanıcısını güncellemek istediğinize emin misiniz?",
                "Onay",
                MessageBoxButtons.YesNo,
                MessageBoxIcon.Question,
                MessageBoxDefaultButton.Button2);
            if (confirm != DialogResult.Yes)
            {
                return;
            }

            SetLoading(true);
            try
            {
                var validation = userService.UpdateUser(user, password);
                if (!validation.IsValid)
                {
                    MessageBox.Show("Lütfen düzeltin:\n\n" + string.Join(Environment.NewLine, validation.Errors.Select(e => "• " + e)), "Hata", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    return;
                }

                LoadUsers();
                MessageBox.Show("Kullanıcı güncellendi.", "Bilgi", MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
            finally
            {
                SetLoading(false);
            }
        }

        private void DeleteUser()
        {
            if (!(usersGrid.CurrentRow?.DataBoundItem is User user))
            {
                MessageBox.Show("Lütfen silinecek kullanıcıyı seçin.", "Uyarı", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            if (MessageBox.Show("Kullanıcı silinecek, emin misiniz?", "Onay", MessageBoxButtons.YesNo, MessageBoxIcon.Question) == DialogResult.Yes)
            {
                SetLoading(true);
                try
                {
                    userService.DeleteUser(user.UserId);
                    LoadUsers();
                    MessageBox.Show("Kullanıcı silindi.", "Bilgi", MessageBoxButtons.OK, MessageBoxIcon.Information);
                }
                finally
                {
                    SetLoading(false);
                }
            }
        }

        private void LoadReports()
        {
            SetLoading(true);
            try
            {
                var today = DateTime.Today;
                var weeklyStart = today.AddDays(-7);
                var monthlyStart = today.AddDays(-30);
                var prevWeeklyStart = weeklyStart.AddDays(-7);
                var prevMonthlyStart = monthlyStart.AddDays(-30);

                dailyCount = requestService.GetDailyBorrowCount(today);
                var weeklyStats = requestService.GetBorrowStats(weeklyStart, today.AddDays(1));
                weeklyCount = weeklyStats.Values.Sum();
                var monthlyStats = requestService.GetBorrowStats(monthlyStart, today.AddDays(1));
                monthlyCount = monthlyStats.Values.Sum();
                allRequests = requestService.GetRequests().ToList();
                ApplyPieRange(pieRangeKey, today);
                var prevWeeklyStats = requestService.GetBorrowStats(prevWeeklyStart, weeklyStart);
                var prevMonthlyStats = requestService.GetBorrowStats(prevMonthlyStart, monthlyStart);
                var prevWeekly = prevWeeklyStats.Values.Sum();
                var prevMonthly = prevMonthlyStats.Values.Sum();

                weeklyChangePercent = CalculateChangePercent(weeklyCount, prevWeekly);
                monthlyChangePercent = CalculateChangePercent(monthlyCount, prevMonthly);

                if (cardDailyValue != null) cardDailyValue.Text = dailyCount.ToString();
                if (cardWeeklyValue != null) cardWeeklyValue.Text = weeklyCount.ToString();
                if (cardMonthlyValue != null) cardMonthlyValue.Text = monthlyCount.ToString();
                if (cardWeeklyTrend != null)
                {
                    cardWeeklyTrend.Text = FormatTrendText(weeklyChangePercent);
                    cardWeeklyTrend.ForeColor = GetTrendColor(weeklyChangePercent);
                }
                if (cardMonthlyTrend != null)
                {
                    cardMonthlyTrend.Text = FormatTrendText(monthlyChangePercent);
                    cardMonthlyTrend.ForeColor = GetTrendColor(monthlyChangePercent);
                }

                statsCanvas.Invalidate();

                trendSeries.Clear();
                var trendStart = today.AddDays(-29);
                for (int i = 0; i < 30; i++)
                {
                    var day = trendStart.AddDays(i);
                    monthlyStats.TryGetValue(day.ToString("yyyy-MM-dd"), out var value);
                    trendSeries.Add(new KeyValuePair<DateTime, int>(day, value));
                }
                trendCanvas.Invalidate();

                var topBooksRaw = requestService.GetTopBooks(10).ToList();
                var topBooks = topBooksRaw.Any()
                    ? topBooksRaw.Select(x => (object)new { Kitap = x.Key, Sayı = x.Value }).ToList()
                    : new List<object> { new { Kitap = "Veri yok", Sayı = 0 } };

                topBooksGrid.DataSource = new BindingSource { DataSource = topBooks };
            }
            catch (Exception)
            {
                lblDaily.Text = string.Empty;
                lblWeekly.Text = string.Empty;
                lblMonthly.Text = string.Empty;
                cardDailyValue.Text = "0";
                cardWeeklyValue.Text = "0";
                cardMonthlyValue.Text = "0";
                weeklyChangePercent = monthlyChangePercent = 0;
                statusPending = statusApproved = statusDelivered = statusReturned = 0;
                if (cardWeeklyTrend != null) { cardWeeklyTrend.Text = "─ %0"; cardWeeklyTrend.ForeColor = Color.Gray; }
                if (cardMonthlyTrend != null) { cardMonthlyTrend.Text = "─ %0"; cardMonthlyTrend.ForeColor = Color.Gray; }
                dailyCount = weeklyCount = monthlyCount = 0;
                statsCanvas.Invalidate();
                trendSeries.Clear();
                trendCanvas.Invalidate();
                topBooksGrid.DataSource = new BindingSource
                {
                    DataSource = new List<object> { new { Kitap = "Hata", Sayı = 0 } }
                };
            }
            finally
            {
                SetLoading(false);
            }
        }

        private void PieRangeButton_Click(object sender, EventArgs e)
        {
            if (sender is Button btn && btn.Tag is string key)
            {
                pieRangeKey = key;
                ApplyPieRange(key, DateTime.Today);
            }
        }

        private void ApplyPieRange(string key, DateTime today)
        {
            DateTime from = key switch
            {
                "1" => today,
                "7" => today.AddDays(-7),
                _ => today.AddDays(-30)
            };

            var filtered = allRequests.Where(r => r.RequestDate.Date >= from && r.RequestDate.Date <= today);
            statusPending = filtered.Count(r => r.Status == RequestStatus.Pending);
            statusApproved = filtered.Count(r => r.Status == RequestStatus.Approved);
            statusDelivered = filtered.Count(r => r.Status == RequestStatus.Delivered);
            statusReturned = filtered.Count(r => r.Status == RequestStatus.Returned);

            UpdatePieButtons();
            statsCanvas.Invalidate();
        }

        private void UpdatePieButtons()
        {
            void Style(Button btn, bool active)
            {
                if (btn == null) return;
                btn.BackColor = active ? Color.FromArgb(220, 235, 252) : SystemColors.Control;
                btn.Font = new Font(btn.Font, active ? FontStyle.Bold : FontStyle.Regular);
            }

            Style(pieBtnToday, pieRangeKey == "1");
            Style(pieBtnWeek, pieRangeKey == "7");
            Style(pieBtnMonth, pieRangeKey == "30");
        }

        private static double CalculateChangePercent(int current, int previous)
        {
            if (previous <= 0)
            {
                return current > 0 ? 100 : 0;
            }
            return ((double)current - previous) / previous * 100;
        }

        private static string FormatTrendText(double changePercent)
        {
            if (Math.Abs(changePercent) < 0.01) return "─ %0";
            var sign = changePercent > 0 ? "↑" : "↓";
            return $"{sign}%{Math.Abs(Math.Round(changePercent))}";
        }

        private static Color GetTrendColor(double changePercent)
        {
            if (changePercent > 0.01) return Color.FromArgb(46, 204, 113);
            if (changePercent < -0.01) return Color.FromArgb(192, 57, 43);
            return Color.Gray;
        }

        private void StylePrimaryButton(Button btn)
        {
            if (btn == null) return;
            btn.FlatStyle = FlatStyle.Flat;
            btn.ForeColor = Color.White;
            btn.BackColor = roleColor;
            btn.FlatAppearance.BorderColor = ControlPaint.Dark(roleColor);
        }

        private void StyleSecondaryButton(Button btn)
        {
            if (btn == null) return;
            btn.FlatStyle = FlatStyle.Flat;
            btn.BackColor = Color.White;
            btn.ForeColor = Color.DimGray;
            btn.FlatAppearance.BorderColor = Color.Gainsboro;
        }

        private void StatsCanvas_Paint(object sender, PaintEventArgs e)
        {
            var g = e.Graphics;
            g.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.AntiAlias;
            g.Clear(Color.WhiteSmoke);

            var slices = new List<(string Label, int Value, Color Color)>
            {
                (RequestStatus.ToDisplay(RequestStatus.Pending), statusPending, Color.FromArgb(90,155,212)),
                (RequestStatus.ToDisplay(RequestStatus.Approved), statusApproved, Color.FromArgb(155,200,100)),
                (RequestStatus.ToDisplay(RequestStatus.Delivered), statusDelivered, Color.FromArgb(246,153,63)),
                (RequestStatus.ToDisplay(RequestStatus.Returned), statusReturned, Color.FromArgb(120,120,120))
            };

            int total = slices.Sum(s => s.Value);
            const int padding = 20;
            const int legendLines = 4;
            int legendHeight = legendLines * 22 + 8;
            int availableWidth = statsCanvas.Width - padding * 2;
            int availableHeight = statsCanvas.Height - padding * 2 - legendHeight;
            int size = Math.Min(availableWidth, availableHeight);
            if (size < 10) size = 10;
            var rect = new Rectangle(padding, padding, size, size);

            if (total == 0)
            {
                var text = "Veri yok";
                var sizeText = g.MeasureString(text, Font);
                g.DrawString(text, Font, Brushes.Gray, (statsCanvas.Width - sizeText.Width) / 2, (statsCanvas.Height - sizeText.Height) / 2);
                return;
            }

            float startAngle = 0;
            foreach (var slice in slices)
            {
                if (slice.Value <= 0) continue;
                float sweep = 360f * slice.Value / total;
                using (var brush = new SolidBrush(slice.Color))
                {
                    g.FillPie(brush, rect, startAngle, sweep);
                }
                using (var pen = new Pen(Color.White, 2))
                {
                    g.DrawPie(pen, rect, startAngle, sweep);
                }
                startAngle += sweep;
            }

            var legendX = rect.Left;
            var legendY = rect.Bottom + 10;
            for (int i = 0; i < slices.Count; i++)
            {
                var slice = slices[i];
                var box = new Rectangle(legendX, legendY + i * 22, 14, 14);
                using (var brush = new SolidBrush(slice.Color))
                {
                    g.FillRectangle(brush, box);
                }
                g.DrawRectangle(Pens.Gray, box);
                var percent = total == 0 ? 0 : Math.Round((double)slice.Value * 100 / total);
                g.DrawString($"{slice.Label} ({slice.Value}, %{percent})", new Font("Segoe UI", 9, FontStyle.Bold), Brushes.Black, box.Right + 6, box.Top - 1);
            }
        }

        private void TrendCanvas_Paint(object sender, PaintEventArgs e)
        {
            var g = e.Graphics;
            g.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.AntiAlias;
            g.Clear(Color.WhiteSmoke);

            if (trendSeries.Count == 0)
            {
                var text = "Veri yok";
                var sizeText = g.MeasureString(text, Font);
                g.DrawString(text, Font, Brushes.Gray, (trendCanvas.Width - sizeText.Width) / 2, (trendCanvas.Height - sizeText.Height) / 2);
                return;
            }

            int max = trendSeries.Max(p => p.Value);
            const int padding = 14;
            const int headerHeight = 36;
            const int bottomPadding = 20;
            int chartWidth = Math.Max(10, trendCanvas.Width - padding * 2);
            int chartHeight = Math.Max(60, trendCanvas.Height - padding - headerHeight - bottomPadding);
            float barWidth = Math.Max(3f, chartWidth / (float)trendSeries.Count);
            float originX = padding;
            float originY = padding + headerHeight + chartHeight;

            g.DrawString("Son 30 Gün Ödünç Trend", new Font("Segoe UI", 9, FontStyle.Bold), Brushes.Black, padding, padding + 4);

            if (max <= 0)
            {
                var text = "Veri yok";
                var sizeText = g.MeasureString(text, Font);
                g.DrawString(text, Font, Brushes.Gray, (trendCanvas.Width - sizeText.Width) / 2, (trendCanvas.Height - sizeText.Height) / 2);
                return;
            }

            // Izgara çizgisi: max ve yarısı
            using (var gridPen = new Pen(Color.Gainsboro, 1))
            {
                gridPen.DashStyle = System.Drawing.Drawing2D.DashStyle.Dot;
                float midY = originY - chartHeight / 2f;
                g.DrawLine(gridPen, originX, midY, originX + chartWidth, midY);
                g.DrawLine(gridPen, originX, originY - chartHeight, originX + chartWidth, originY - chartHeight);
                g.DrawString($"Max {max}", new Font("Segoe UI", 8, FontStyle.Regular), Brushes.DimGray, originX, originY - chartHeight + 2);
                g.DrawString($"~{Math.Max(1, max / 2)}", new Font("Segoe UI", 8, FontStyle.Regular), Brushes.DimGray, originX, midY - 10);
            }

            using (var brush = new SolidBrush(Color.FromArgb(90, 155, 212)))
            {
                for (int i = 0; i < trendSeries.Count; i++)
                {
                    var value = trendSeries[i].Value;
                    float height = (float)value / max * chartHeight;
                    var rect = new RectangleF(originX + i * barWidth, originY - height, barWidth - 1, height);
                    g.FillRectangle(brush, rect);
                    if (height >= 14)
                    {
                        var label = value.ToString();
                        var sizeText = g.MeasureString(label, Font);
                        g.DrawString(label, new Font("Segoe UI", 7, FontStyle.Bold), Brushes.DimGray, rect.Left, rect.Top - sizeText.Height);
                    }
                }
            }

            var startText = trendSeries.First().Key.ToString("dd.MM");
            var endText = trendSeries.Last().Key.ToString("dd.MM");
            var startSize = g.MeasureString(startText, Font);
            var endSize = g.MeasureString(endText, Font);
            g.DrawString(startText, Font, Brushes.DimGray, originX, originY + 4);
            g.DrawString(endText, Font, Brushes.DimGray, trendCanvas.Width - padding - endSize.Width, originY + 4);
        }

        private void PositionLogoutButton(Panel container)
        {
            const int padding = 10;
            logoutButton.Location = new Point(container.ClientSize.Width - logoutButton.Width - padding, padding);
        }

        private void PositionStatusLabel(Panel container)
        {
            const int padding = 10;
            statusLabel.Location = new Point(
                Math.Max(container.ClientSize.Width - statusLabel.Width - padding, 0),
                Math.Max(container.ClientSize.Height - statusLabel.Height - padding, 0));
        }

        private void SetLoading(bool start)
        {
            const int minVisibleMs = 500;

            if (start)
            {
                loadingDepth = Math.Max(0, loadingDepth + 1);
                if (loadingDepth == 1)
                {
                    loadingStartedAt = DateTime.Now;
                    statusLabel.Visible = true;
                    UseWaitCursor = true;
                    Cursor.Current = Cursors.WaitCursor;
                    loadingTimer?.Stop();
                }
                return;
            }

            if (loadingDepth <= 0) return;
            loadingDepth = Math.Max(0, loadingDepth - 1);
            if (loadingDepth > 0) return;

            var elapsed = DateTime.Now - loadingStartedAt;
            var remaining = minVisibleMs - (int)elapsed.TotalMilliseconds;
            if (remaining <= 0)
            {
                HideLoading();
                return;
            }

            if (loadingTimer == null)
            {
                loadingTimer = new Timer();
                loadingTimer.Tick += (_, __) =>
                {
                    loadingTimer.Stop();
                    HideLoading();
                };
            }

            loadingTimer.Interval = Math.Max(50, remaining);
            loadingTimer.Start();
        }

        private void HideLoading()
        {
            statusLabel.Visible = false;
            UseWaitCursor = false;
            Cursor.Current = Cursors.Default;
        }

        private void Logout()
        {
            Close();
        }
    }
}
