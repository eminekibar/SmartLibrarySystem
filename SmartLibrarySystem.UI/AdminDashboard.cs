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

        private DataGridView booksGrid;
        private TextBox txtTitle;
        private TextBox txtAuthor;
        private TextBox txtCategory;
        private NumericUpDown numYear;
        private NumericUpDown numStock;
        private TextBox txtShelf;

        private DataGridView usersGrid;
        private TextBox txtUserName;
        private TextBox txtUserEmail;
        private TextBox txtUserSchool;
        private TextBox txtUserPhone;
        private TextBox txtUserPassword;
        private ComboBox cmbUserRole;

        private DataGridView topBooksGrid;
        private Label lblDaily;
        private Label lblWeekly;
        private Label lblMonthly;
        private Button logoutButton;

        public AdminDashboard(User user)
        {
            currentUser = user;
            InitializeComponent();
            LoadBooks();
            LoadUsers();
            LoadReports();
        }

        private void InitializeComponent()
        {
            Text = $"Admin Paneli - {currentUser.FullName}";
            WindowState = FormWindowState.Normal;
            StartPosition = FormStartPosition.CenterScreen;
            Size = new Size(1200, 800);

            var tabs = new TabControl { Dock = DockStyle.Fill };
            tabs.TabPages.Add(CreateBooksTab());
            tabs.TabPages.Add(CreateUsersTab());
            tabs.TabPages.Add(CreateReportsTab());

            logoutButton = new Button
            {
                Text = "Çıkış Yap",
                AutoSize = true,
                Anchor = AnchorStyles.Top | AnchorStyles.Right
            };
            logoutButton.Click += (_, __) => Logout();

            var container = new Panel { Dock = DockStyle.Fill };
            container.Controls.Add(tabs);
            container.Controls.Add(logoutButton);
            logoutButton.BringToFront();
            container.Resize += (_, __) => PositionLogoutButton(container);
            PositionLogoutButton(container);

            Controls.Add(container);
        }

        private TabPage CreateBooksTab()
        {
            var tab = new TabPage("Kitap Yönetimi");

            var layout = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                RowCount = 1,
                ColumnCount = 2
            };
            layout.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
            layout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 70));
            layout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 30));

            booksGrid = new DataGridView
            {
                Dock = DockStyle.Fill,
                ReadOnly = true,
                AutoGenerateColumns = true,
                SelectionMode = DataGridViewSelectionMode.FullRowSelect
            };
            booksGrid.SelectionChanged += (_, __) => FillBookForm();

            layout.Controls.Add(booksGrid, 0, 0);
            layout.Controls.Add(CreateBookFormPanel(), 1, 0);

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
            numYear = new NumericUpDown { Minimum = 0, Maximum = 2100, Value = 2024, Dock = DockStyle.Fill };
            numStock = new NumericUpDown { Minimum = 0, Maximum = 1000, Value = 1, Dock = DockStyle.Fill };
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
            panel.Controls.Add(new Label { Text = "Raf", AutoSize = true }, 0, 5);
            panel.Controls.Add(txtShelf, 1, 5);

            var addButton = new Button { Text = "Ekle", Dock = DockStyle.Fill };
            addButton.Click += (_, __) => AddBook();
            var updateButton = new Button { Text = "Güncelle", Dock = DockStyle.Fill };
            updateButton.Click += (_, __) => UpdateBook();
            var deleteButton = new Button { Text = "Sil", Dock = DockStyle.Fill };
            deleteButton.Click += (_, __) => DeleteBook();
            var clearButton = new Button { Text = "Temizle", Dock = DockStyle.Fill };
            clearButton.Click += (_, __) => ClearBookForm();

            var buttonsPanel = new TableLayoutPanel
            {
                Dock = DockStyle.Top,
                ColumnCount = 1,
                RowCount = 4,
                AutoSize = true,
                AutoSizeMode = AutoSizeMode.GrowAndShrink
            };
            buttonsPanel.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
            for (int i = 0; i < 4; i++)
            {
                buttonsPanel.RowStyles.Add(new RowStyle(SizeType.AutoSize));
            }

            buttonsPanel.Controls.Add(addButton, 0, 0);
            buttonsPanel.Controls.Add(updateButton, 0, 1);
            buttonsPanel.Controls.Add(deleteButton, 0, 2);
            buttonsPanel.Controls.Add(clearButton, 0, 3);

            panel.Controls.Add(buttonsPanel, 0, 6);
            panel.SetColumnSpan(buttonsPanel, 2);

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
                AutoGenerateColumns = true,
                SelectionMode = DataGridViewSelectionMode.FullRowSelect
            };

            layout.Controls.Add(usersGrid, 0, 0);
            layout.Controls.Add(CreateUserFormPanel(), 1, 0);
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
            txtUserPassword = new TextBox { PasswordChar = '*', Dock = DockStyle.Fill };
            cmbUserRole = new ComboBox { DropDownStyle = ComboBoxStyle.DropDownList, Dock = DockStyle.Fill };
            cmbUserRole.Items.AddRange(new[] { RoleConstants.Student, RoleConstants.Staff, RoleConstants.Admin });
            cmbUserRole.SelectedIndex = 0;

            panel.Controls.Add(new Label { Text = "Ad Soyad", AutoSize = true }, 0, 0);
            panel.Controls.Add(txtUserName, 1, 0);
            panel.Controls.Add(new Label { Text = "Email", AutoSize = true }, 0, 1);
            panel.Controls.Add(txtUserEmail, 1, 1);
            panel.Controls.Add(new Label { Text = "Okul No", AutoSize = true }, 0, 2);
            panel.Controls.Add(txtUserSchool, 1, 2);
            panel.Controls.Add(new Label { Text = "Telefon", AutoSize = true }, 0, 3);
            panel.Controls.Add(txtUserPhone, 1, 3);
            panel.Controls.Add(new Label { Text = "Şifre", AutoSize = true }, 0, 4);
            panel.Controls.Add(txtUserPassword, 1, 4);
            panel.Controls.Add(new Label { Text = "Rol", AutoSize = true }, 0, 5);
            panel.Controls.Add(cmbUserRole, 1, 5);

            var addButton = new Button { Text = "Kullanıcı Ekle", Dock = DockStyle.Fill };
            addButton.Click += (_, __) => AddUser();
            var deleteButton = new Button { Text = "Seçileni Sil", Dock = DockStyle.Fill };
            deleteButton.Click += (_, __) => DeleteUser();
            var refreshButton = new Button { Text = "Yenile", Dock = DockStyle.Fill };
            refreshButton.Click += (_, __) => LoadUsers();

            var buttonsPanel = new TableLayoutPanel
            {
                Dock = DockStyle.Top,
                ColumnCount = 1,
                RowCount = 3,
                AutoSize = true,
                AutoSizeMode = AutoSizeMode.GrowAndShrink
            };
            buttonsPanel.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
            for (int i = 0; i < 3; i++)
            {
                buttonsPanel.RowStyles.Add(new RowStyle(SizeType.AutoSize));
            }

            buttonsPanel.Controls.Add(addButton, 0, 0);
            buttonsPanel.Controls.Add(deleteButton, 0, 1);
            buttonsPanel.Controls.Add(refreshButton, 0, 2);

            panel.Controls.Add(buttonsPanel, 0, 6);
            panel.SetColumnSpan(buttonsPanel, 2);

            return panel;
        }

        private TabPage CreateReportsTab()
        {
            var tab = new TabPage("Raporlar");
            var layout = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                RowCount = 2,
                ColumnCount = 2,
                Padding = new Padding(10)
            };
            layout.RowStyles.Add(new RowStyle(SizeType.Percent, 40));
            layout.RowStyles.Add(new RowStyle(SizeType.Percent, 60));
            layout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50));
            layout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50));

            var statsPanel = new FlowLayoutPanel
            {
                Dock = DockStyle.Fill,
                FlowDirection = FlowDirection.TopDown
            };
            lblDaily = new Label { Font = new Font("Segoe UI", 11, FontStyle.Bold), AutoSize = true };
            lblWeekly = new Label { Font = new Font("Segoe UI", 11, FontStyle.Bold), AutoSize = true };
            lblMonthly = new Label { Font = new Font("Segoe UI", 11, FontStyle.Bold), AutoSize = true };

            statsPanel.Controls.Add(lblDaily);
            statsPanel.Controls.Add(lblWeekly);
            statsPanel.Controls.Add(lblMonthly);

            topBooksGrid = new DataGridView
            {
                Dock = DockStyle.Fill,
                ReadOnly = true,
                AutoGenerateColumns = true,
                SelectionMode = DataGridViewSelectionMode.FullRowSelect
            };

            var refreshButton = new Button { Text = "Raporları Yenile", Dock = DockStyle.Top, Height = 40 };
            refreshButton.Click += (_, __) => LoadReports();

            layout.Controls.Add(statsPanel, 0, 0);
            layout.SetColumnSpan(statsPanel, 2);
            layout.Controls.Add(refreshButton, 0, 1);
            layout.Controls.Add(topBooksGrid, 1, 1);

            return tab;
        }

        private void LoadBooks()
        {
            booksGrid.DataSource = new BindingSource { DataSource = new List<Book>(bookService.GetAll()) };
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

            var validation = bookService.AddBook(book);
            if (!validation.IsValid)
            {
                MessageBox.Show(string.Join(Environment.NewLine, validation.Errors), "Hata", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return;
            }

            LoadBooks();
            ClearBookForm();
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

            var validation = bookService.UpdateBook(selected);
            if (!validation.IsValid)
            {
                MessageBox.Show(string.Join(Environment.NewLine, validation.Errors), "Hata", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return;
            }

            LoadBooks();
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
                bookService.DeleteBook(selected.BookId);
                LoadBooks();
                ClearBookForm();
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
            usersGrid.DataSource = new BindingSource { DataSource = new List<User>(userService.GetAll()) };
        }

        private void AddUser()
        {
            var user = new User
            {
                FullName = txtUserName.Text.Trim(),
                Email = txtUserEmail.Text.Trim(),
                SchoolNumber = txtUserSchool.Text.Trim(),
                Phone = txtUserPhone.Text.Trim(),
                Role = cmbUserRole.SelectedItem.ToString()
            };

            var validation = userService.Register(user, txtUserPassword.Text);
            if (!validation.IsValid)
            {
                MessageBox.Show(string.Join(Environment.NewLine, validation.Errors), "Hata", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return;
            }

            LoadUsers();
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
                userService.DeleteUser(user.UserId);
                LoadUsers();
            }
        }

        private void LoadReports()
        {
            var today = DateTime.Today;
            var weeklyStart = today.AddDays(-7);
            var monthlyStart = today.AddDays(-30);

            lblDaily.Text = $"Günlük ödünç: {requestService.GetDailyBorrowCount(today)}";

            var weeklyStats = requestService.GetBorrowStats(weeklyStart, today.AddDays(1));
            lblWeekly.Text = $"Son 7 gün: {weeklyStats.Values.Sum()}";

            var monthlyStats = requestService.GetBorrowStats(monthlyStart, today.AddDays(1));
            lblMonthly.Text = $"Son 30 gün: {monthlyStats.Values.Sum()}";

            topBooksGrid.DataSource = new BindingSource
            {
                DataSource = requestService.GetTopBooks(5)
                    .Select(x => new { Kitap = x.Key, Sayı = x.Value })
                    .ToList()
            };
        }

        private void PositionLogoutButton(Panel container)
        {
            const int padding = 10;
            logoutButton.Location = new Point(container.ClientSize.Width - logoutButton.Width - padding, padding);
        }

        private void Logout()
        {
            var login = new LoginForm();
            login.Show();
            Close();
        }
    }
}
