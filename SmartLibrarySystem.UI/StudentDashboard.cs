using System;
using System.Collections.Generic;
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
        private TextBox categoryFilter;
        private TextBox authorFilter;
        private TextBox keywordFilter;
        private NumericUpDown yearFilter;

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
            WindowState = FormWindowState.Maximized;

            var tabControl = new TabControl { Dock = DockStyle.Fill };
            tabControl.TabPages.Add(CreateBooksTab());
            tabControl.TabPages.Add(CreateRequestsTab());

            Controls.Add(tabControl);
        }

        private TabPage CreateBooksTab()
        {
            var tab = new TabPage("Kitaplar");

            var filterPanel = new FlowLayoutPanel
            {
                Dock = DockStyle.Top,
                Height = 60,
                Padding = new Padding(10),
                FlowDirection = FlowDirection.LeftToRight
            };

            categoryFilter = new TextBox { Width = 120 };
            authorFilter = new TextBox { Width = 120 };
            keywordFilter = new TextBox { Width = 160 };
            yearFilter = new NumericUpDown { Width = 80, Minimum = 0, Maximum = 2100, Value = 0 };

            var searchButton = new Button { Text = "Ara", Width = 80 };
            searchButton.Click += (_, __) => LoadBooks();
            var clearButton = new Button { Text = "Temizle", Width = 80 };
            clearButton.Click += (_, __) =>
            {
                categoryFilter.Text = string.Empty;
                authorFilter.Text = string.Empty;
                keywordFilter.Text = string.Empty;
                yearFilter.Value = 0;
                LoadBooks();
            };

            filterPanel.Controls.Add(new Label { Text = "Kategori:", AutoSize = true, TextAlign = ContentAlignment.MiddleLeft });
            filterPanel.Controls.Add(categoryFilter);
            filterPanel.Controls.Add(new Label { Text = "Yazar:", AutoSize = true, TextAlign = ContentAlignment.MiddleLeft });
            filterPanel.Controls.Add(authorFilter);
            filterPanel.Controls.Add(new Label { Text = "Yıl:", AutoSize = true, TextAlign = ContentAlignment.MiddleLeft });
            filterPanel.Controls.Add(yearFilter);
            filterPanel.Controls.Add(keywordFilter);
            filterPanel.Controls.Add(searchButton);
            filterPanel.Controls.Add(clearButton);

            booksGrid = new DataGridView
            {
                Dock = DockStyle.Fill,
                ReadOnly = true,
                AutoGenerateColumns = true,
                SelectionMode = DataGridViewSelectionMode.FullRowSelect
            };

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

            tab.Controls.Add(booksGrid);
            tab.Controls.Add(actionPanel);
            tab.Controls.Add(filterPanel);
            return tab;
        }

        private TabPage CreateRequestsTab()
        {
            var tab = new TabPage("Taleplerim");

            requestsGrid = new DataGridView
            {
                Dock = DockStyle.Fill,
                ReadOnly = true,
                AutoGenerateColumns = true,
                SelectionMode = DataGridViewSelectionMode.FullRowSelect
            };

            var refreshButton = new Button { Text = "Yenile", Dock = DockStyle.Top, Height = 35 };
            refreshButton.Click += (_, __) => LoadRequests();

            tab.Controls.Add(requestsGrid);
            tab.Controls.Add(refreshButton);
            return tab;
        }

        private void LoadBooks()
        {
            int? year = yearFilter.Value == 0 ? (int?)null : (int)yearFilter.Value;
            var books = bookService.Search(categoryFilter.Text, authorFilter.Text, year, keywordFilter.Text);
            booksGrid.DataSource = new BindingSource { DataSource = new List<Book>(books) };
        }

        private void LoadRequests()
        {
            var requests = requestService.GetUserRequests(currentUser.UserId);
            requestsGrid.DataSource = new BindingSource { DataSource = new List<BorrowRequest>(requests) };
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

            var validation = requestService.CreateRequest(currentUser.UserId, book.BookId);
            if (!validation.IsValid)
            {
                MessageBox.Show(string.Join(Environment.NewLine, validation.Errors), "Hata", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return;
            }

            MessageBox.Show("Ödünç talebi oluşturuldu.", "Bilgi", MessageBoxButtons.OK, MessageBoxIcon.Information);
            LoadRequests();
        }
    }
}
