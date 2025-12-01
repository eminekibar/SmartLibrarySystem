using System;
using System.Drawing;
using System.Windows.Forms;
using SmartLibrarySystem.BLL;
using SmartLibrarySystem.Models;

namespace SmartLibrarySystem.UI
{
    public class BookDetailsForm : Form
    {
        private readonly Book book;
        private readonly User user;
        private readonly RequestService requestService;
        private readonly Label messageLabel = new Label();

        public BookDetailsForm(Book book, User user, RequestService requestService)
        {
            this.book = book;
            this.user = user;
            this.requestService = requestService;
            InitializeComponent();
        }

        private void InitializeComponent()
        {
            Text = "Kitap Detayı";
            Size = new Size(420, 320);
            StartPosition = FormStartPosition.CenterParent;
            FormBorderStyle = FormBorderStyle.FixedDialog;
            MaximizeBox = false;

            var table = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                ColumnCount = 2,
                RowCount = 7,
                Padding = new Padding(20)
            };
            table.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 35));
            table.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 65));

            table.Controls.Add(new Label { Text = "Başlık:", AutoSize = true }, 0, 0);
            table.Controls.Add(new Label { Text = book.Title, AutoSize = true }, 1, 0);
            table.Controls.Add(new Label { Text = "Yazar:", AutoSize = true }, 0, 1);
            table.Controls.Add(new Label { Text = book.Author, AutoSize = true }, 1, 1);
            table.Controls.Add(new Label { Text = "Kategori:", AutoSize = true }, 0, 2);
            table.Controls.Add(new Label { Text = book.Category, AutoSize = true }, 1, 2);
            table.Controls.Add(new Label { Text = "Yıl:", AutoSize = true }, 0, 3);
            table.Controls.Add(new Label { Text = book.PublishYear.ToString(), AutoSize = true }, 1, 3);
            table.Controls.Add(new Label { Text = "Stok:", AutoSize = true }, 0, 4);
            table.Controls.Add(new Label { Text = book.Stock.ToString(), AutoSize = true }, 1, 4);
            table.Controls.Add(new Label { Text = "Raf:", AutoSize = true }, 0, 5);
            table.Controls.Add(new Label { Text = book.Shelf, AutoSize = true }, 1, 5);

            var requestButton = new Button { Text = "Ödünç Talebi Gönder", Dock = DockStyle.Fill, Enabled = user != null };
            requestButton.Click += RequestButton_Click;
            table.Controls.Add(requestButton, 0, 6);
            table.SetColumnSpan(requestButton, 2);

            messageLabel.ForeColor = Color.Red;
            messageLabel.AutoSize = true;
            messageLabel.Dock = DockStyle.Bottom;

            Controls.Add(table);
            Controls.Add(messageLabel);
        }

        private void RequestButton_Click(object sender, EventArgs e)
        {
            messageLabel.Text = string.Empty;
            var result = requestService.CreateRequest(user.UserId, book.BookId);
            if (!result.IsValid)
            {
                messageLabel.Text = string.Join(Environment.NewLine, result.Errors);
                return;
            }

            MessageBox.Show("Ödünç talebi gönderildi.", "Bilgi", MessageBoxButtons.OK, MessageBoxIcon.Information);
            Close();
        }
    }
}
