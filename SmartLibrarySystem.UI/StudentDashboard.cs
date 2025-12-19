using System;
using System.Collections.Generic;
using System.Linq;
using System.Drawing;
using System.Windows.Forms;
using SmartLibrarySystem.BLL;
using SmartLibrarySystem.Models;

namespace SmartLibrarySystem.UI
{
    public class StudentDashboard : Form
    {
        private readonly User currentUser;
        private readonly BookService bookService = new BookService();
        private readonly RequestService requestService = new RequestService();

        private DataGridView booksGrid;
        private DataGridView requestsGrid;
        private TextBox titleFilter;
        private TextBox authorFilter;
        private TextBox categoryFilter;
        private NumericUpDown yearFilter;
        private TextBox quickSearchBox;
        private List<Book> allBooks = new List<Book>();
        private Button logoutButton;
        private Label statusLabel;
        private int loadingDepth;
        private DateTime loadingStartedAt;
        private Timer loadingTimer;
        private const string DateTimeDisplayFormat = "dd.MM.yyyy HH:mm";

        public StudentDashboard(User user)
        {
            currentUser = user;
            InitializeComponent();
            LoadBooks();
            LoadRequests();
        }

        private void InitializeComponent()
        {
            Text = $"Öğrenci Paneli - {currentUser.FullName}";
            WindowState = FormWindowState.Normal;
            StartPosition = FormStartPosition.CenterScreen;
            Size = new Size(1200, 800);

            logoutButton = new Button
            {
                Text = "Çıkış Yap",
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

            var tabControl = new TabControl { Dock = DockStyle.Fill };
            tabControl.TabPages.Add(CreateBooksTab());
            tabControl.TabPages.Add(CreateRequestsTab());

            var container = new Panel { Dock = DockStyle.Fill };
            container.Controls.Add(tabControl);
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
            var tab = new TabPage("Kitaplar");

            var layout = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                RowCount = 2,
                ColumnCount = 2
            };
            layout.RowStyles.Add(new RowStyle(SizeType.Percent, 80));
            layout.RowStyles.Add(new RowStyle(SizeType.Percent, 20));
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

            var actionPanel = new FlowLayoutPanel
            {
                Dock = DockStyle.Bottom,
                Height = 50,
                FlowDirection = FlowDirection.LeftToRight,
                Padding = new Padding(10)
            };

            var detailsButton = new Button { Text = "Detay", Width = 100 };
            detailsButton.Click += (_, __) => ShowDetails();
            var requestButton = new Button { Text = "Ödünç Talebi Gönder", Width = 160 };
            requestButton.Click += (_, __) => SendRequest();

            actionPanel.Controls.Add(detailsButton);
            actionPanel.Controls.Add(requestButton);

            // Sağ panel: üstte hızlı arama, altında dikey filtre
            var rightPanel = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                RowCount = 2,
                ColumnCount = 1,
                Padding = new Padding(10)
            };
            rightPanel.RowStyles.Add(new RowStyle(SizeType.AutoSize));
            rightPanel.RowStyles.Add(new RowStyle(SizeType.Percent, 100));

            // Hızlı arama
            var quickPanel = new FlowLayoutPanel
            {
                Dock = DockStyle.Top,
                FlowDirection = FlowDirection.LeftToRight,
                AutoSize = true,
                AutoSizeMode = AutoSizeMode.GrowAndShrink
            };
            quickPanel.Controls.Add(new Label { Text = "Hızlı Arama:", AutoSize = true, Margin = new Padding(0, 6, 4, 0) });
            quickSearchBox = new TextBox { Width = 220 };
            quickPanel.Controls.Add(quickSearchBox);

            // Dikey filtreler
            var filterPanel = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                ColumnCount = 2,
                RowCount = 6,
                AutoSize = true,
                AutoSizeMode = AutoSizeMode.GrowAndShrink,
                Padding = new Padding(0, 8, 0, 0)
            };
            filterPanel.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));
            filterPanel.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
            for (int i = 0; i < 6; i++)
            {
                filterPanel.RowStyles.Add(new RowStyle(SizeType.AutoSize));
            }

            titleFilter = new TextBox { Dock = DockStyle.Fill, Width = 200 };
            authorFilter = new TextBox { Dock = DockStyle.Fill, Width = 200 };
            categoryFilter = new TextBox { Dock = DockStyle.Fill, Width = 200 };
            yearFilter = new NumericUpDown { Dock = DockStyle.Left, Minimum = 0, Maximum = 2100, Width = 100 };

            filterPanel.Controls.Add(new Label { Text = "Kitap Adı", AutoSize = true }, 0, 0);
            filterPanel.Controls.Add(titleFilter, 1, 0);
            filterPanel.Controls.Add(new Label { Text = "Yazar", AutoSize = true }, 0, 1);
            filterPanel.Controls.Add(authorFilter, 1, 1);
            filterPanel.Controls.Add(new Label { Text = "Kategori", AutoSize = true }, 0, 2);
            filterPanel.Controls.Add(categoryFilter, 1, 2);
            filterPanel.Controls.Add(new Label { Text = "Yıl", AutoSize = true }, 0, 3);
            filterPanel.Controls.Add(yearFilter, 1, 3);

            var buttonPanel = new FlowLayoutPanel
            {
                Dock = DockStyle.Fill,
                FlowDirection = FlowDirection.LeftToRight,
                AutoSize = true,
                AutoSizeMode = AutoSizeMode.GrowAndShrink,
                Padding = new Padding(0, 8, 0, 0)
            };
            var searchButton = new Button { Text = "Ara", AutoSize = true };
            searchButton.Click += (_, __) => ApplyBookFilter();
            var clearButton = new Button { Text = "Filtreyi Temizle", AutoSize = true, Margin = new Padding(8, 0, 0, 0) };
            clearButton.Click += (_, __) =>
            {
                quickSearchBox.Text = string.Empty;
                titleFilter.Text = string.Empty;
                authorFilter.Text = string.Empty;
                categoryFilter.Text = string.Empty;
                yearFilter.Value = 0;
                ApplyBookFilter();
            };
            buttonPanel.Controls.Add(searchButton);
            buttonPanel.Controls.Add(clearButton);
            filterPanel.Controls.Add(buttonPanel, 1, 4);

            rightPanel.Controls.Add(quickPanel, 0, 0);
            rightPanel.Controls.Add(filterPanel, 0, 1);

            layout.Controls.Add(booksGrid, 0, 0);
            layout.SetRowSpan(booksGrid, 2);
            layout.Controls.Add(rightPanel, 1, 0);
            layout.SetRowSpan(rightPanel, 2);
            layout.Controls.Add(actionPanel, 0, 1);

            tab.Controls.Add(layout);
            return tab;
        }

        private TabPage CreateRequestsTab()
        {
            var tab = new TabPage("Taleplerim");

            requestsGrid = new DataGridView
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
            requestsGrid.Columns.Add(new DataGridViewTextBoxColumn { DataPropertyName = nameof(BorrowRequest.RequestId), HeaderText = "Talep No", Width = 80 });
            requestsGrid.Columns.Add(new DataGridViewTextBoxColumn { DataPropertyName = nameof(BorrowRequest.BookTitle), HeaderText = "Kitap" });
            requestsGrid.Columns.Add(new DataGridViewTextBoxColumn { DataPropertyName = nameof(BorrowRequest.BookAuthor), HeaderText = "Yazar" });
            requestsGrid.Columns.Add(new DataGridViewTextBoxColumn { DataPropertyName = nameof(BorrowRequest.Status), HeaderText = "Durum", Width = 120 });
            requestsGrid.Columns.Add(new DataGridViewTextBoxColumn { DataPropertyName = nameof(BorrowRequest.RequestDate), HeaderText = "Talep Tarihi", Width = 130 });
            requestsGrid.Columns.Add(new DataGridViewTextBoxColumn { DataPropertyName = nameof(BorrowRequest.DeliveryDate), HeaderText = "Teslim Tarihi", Width = 130 });
            requestsGrid.Columns.Add(new DataGridViewTextBoxColumn { DataPropertyName = nameof(BorrowRequest.ReturnDate), HeaderText = "İade Tarihi", Width = 130 });
            requestsGrid.CellFormatting += RequestsGrid_CellFormatting;

            var refreshButton = new Button { Text = "Yenile", Dock = DockStyle.Top, Height = 35 };
            refreshButton.Click += (_, __) => LoadRequests();

            tab.Controls.Add(requestsGrid);
            tab.Controls.Add(refreshButton);
            return tab;
        }

        private void LoadBooks()
        {
            SetLoading(true);
            try
            {
                allBooks = new List<Book>(bookService.GetAll());
                ApplyBookFilter();
            }
            finally
            {
                SetLoading(false);
            }
        }

        private void ApplyBookFilter()
        {
            SetLoading(true);
            try
            {
                var year = yearFilter.Value == 0 ? (int?)null : (int)yearFilter.Value;
                var quick = quickSearchBox.Text.Trim();
                var title = titleFilter.Text.Trim();
                var author = authorFilter.Text.Trim();
                var category = categoryFilter.Text.Trim();

                IEnumerable<Book> filtered = allBooks;
                if (!string.IsNullOrWhiteSpace(quick))
                {
                    filtered = filtered.Where(b =>
                        (b.Title?.IndexOf(quick, StringComparison.OrdinalIgnoreCase) ?? -1) >= 0 ||
                        (b.Author?.IndexOf(quick, StringComparison.OrdinalIgnoreCase) ?? -1) >= 0 ||
                        (b.Category?.IndexOf(quick, StringComparison.OrdinalIgnoreCase) ?? -1) >= 0);
                }

                filtered = filtered.Where(b =>
                    (string.IsNullOrWhiteSpace(title) || (b.Title?.IndexOf(title, StringComparison.OrdinalIgnoreCase) ?? -1) >= 0) &&
                    (string.IsNullOrWhiteSpace(author) || (b.Author?.IndexOf(author, StringComparison.OrdinalIgnoreCase) ?? -1) >= 0) &&
                    (string.IsNullOrWhiteSpace(category) || (b.Category?.IndexOf(category, StringComparison.OrdinalIgnoreCase) ?? -1) >= 0) &&
                    (!year.HasValue || b.PublishYear == year.Value)
                );

                booksGrid.DataSource = new BindingSource { DataSource = filtered.ToList() };
            }
            finally
            {
                SetLoading(false);
            }
        }

        private void LoadRequests()
        {
            SetLoading(true);
            try
            {
                var requests = requestService.GetUserRequests(currentUser.UserId);
                requestsGrid.DataSource = new BindingSource { DataSource = new List<BorrowRequest>(requests) };
            }
            finally
            {
                SetLoading(false);
            }
        }

        private void RequestsGrid_CellFormatting(object sender, DataGridViewCellFormattingEventArgs e)
        {
            var property = requestsGrid.Columns[e.ColumnIndex].DataPropertyName;
            if (property == nameof(BorrowRequest.Status) && e.Value is string status)
            {
                e.Value = RequestStatus.ToDisplay(status);
                e.FormattingApplied = true;
                return;
            }

            if (e.Value is DateTime dt)
            {
                e.Value = dt.ToString(DateTimeDisplayFormat);
                e.FormattingApplied = true;
            }
        }

        private Book GetSelectedBook()
        {
            if (booksGrid.CurrentRow?.DataBoundItem is Book book)
            {
                return book;
            }
            MessageBox.Show("Lütfen bir kitap seçiniz.", "Uyarı", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            return null;
        }

        private void ShowDetails()
        {
            var book = GetSelectedBook();
            if (book == null) return;

            using (var form = new BookDetailsForm(book, currentUser, requestService))
            {
                form.ShowDialog(this);
                LoadRequests();
            }
        }

        private void SendRequest()
        {
            var book = GetSelectedBook();
            if (book == null) return;

            var confirm = MessageBox.Show(
                $"{book.Title} için ödünç talebi göndermek istediğinize emin misiniz?",
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
                var validation = requestService.CreateRequest(currentUser.UserId, book.BookId);
                if (!validation.IsValid)
                {
                    MessageBox.Show(string.Join(Environment.NewLine, validation.Errors), "Hata", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    return;
                }

                MessageBox.Show("Ödünç talebi oluşturuldu.", "Bilgi", MessageBoxButtons.OK, MessageBoxIcon.Information);
                LoadRequests();
            }
            finally
            {
                SetLoading(false);
            }
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
