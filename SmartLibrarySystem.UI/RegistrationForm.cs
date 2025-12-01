using System;
using System.Drawing;
using System.Windows.Forms;
using SmartLibrarySystem.BLL;
using SmartLibrarySystem.Models;

namespace SmartLibrarySystem.UI
{
    public class RegistrationForm : Form
    {
        private readonly UserService userService = new UserService();
        private readonly TextBox fullNameTextBox = new TextBox();
        private readonly TextBox emailTextBox = new TextBox();
        private readonly TextBox schoolNumberTextBox = new TextBox();
        private readonly TextBox phoneTextBox = new TextBox();
        private readonly TextBox passwordTextBox = new TextBox();
        private readonly TextBox confirmPasswordTextBox = new TextBox();
        private readonly Label messageLabel = new Label();

        public RegistrationForm()
        {
            InitializeComponent();
        }

        private void InitializeComponent()
        {
            Text = "Öğrenci Kayıt";
            Size = new Size(520, 420);
            StartPosition = FormStartPosition.CenterParent;

            passwordTextBox.PasswordChar = '*';
            confirmPasswordTextBox.PasswordChar = '*';

            var table = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                ColumnCount = 2,
                RowCount = 7,
                Padding = new Padding(20),
                AutoSize = true
            };
            table.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 35));
            table.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 65));

            table.Controls.Add(new Label { Text = "Ad Soyad:", TextAlign = ContentAlignment.MiddleLeft, Dock = DockStyle.Fill }, 0, 0);
            table.Controls.Add(fullNameTextBox, 1, 0);
            table.Controls.Add(new Label { Text = "Email:", TextAlign = ContentAlignment.MiddleLeft, Dock = DockStyle.Fill }, 0, 1);
            table.Controls.Add(emailTextBox, 1, 1);
            table.Controls.Add(new Label { Text = "Okul No:", TextAlign = ContentAlignment.MiddleLeft, Dock = DockStyle.Fill }, 0, 2);
            table.Controls.Add(schoolNumberTextBox, 1, 2);
            table.Controls.Add(new Label { Text = "Telefon:", TextAlign = ContentAlignment.MiddleLeft, Dock = DockStyle.Fill }, 0, 3);
            table.Controls.Add(phoneTextBox, 1, 3);
            table.Controls.Add(new Label { Text = "Şifre:", TextAlign = ContentAlignment.MiddleLeft, Dock = DockStyle.Fill }, 0, 4);
            table.Controls.Add(passwordTextBox, 1, 4);
            table.Controls.Add(new Label { Text = "Şifre Tekrar:", TextAlign = ContentAlignment.MiddleLeft, Dock = DockStyle.Fill }, 0, 5);
            table.Controls.Add(confirmPasswordTextBox, 1, 5);

            var registerButton = new Button { Text = "Kaydol", Dock = DockStyle.Fill };
            registerButton.Click += RegisterButton_Click;
            table.Controls.Add(registerButton, 0, 6);
            table.SetColumnSpan(registerButton, 2);

            messageLabel.ForeColor = Color.Red;
            messageLabel.AutoSize = true;
            messageLabel.Dock = DockStyle.Bottom;

            Controls.Add(table);
            Controls.Add(messageLabel);

            AcceptButton = registerButton;
        }

        private void RegisterButton_Click(object sender, EventArgs e)
        {
            messageLabel.Text = string.Empty;

            if (!string.Equals(passwordTextBox.Text, confirmPasswordTextBox.Text))
            {
                messageLabel.Text = "Şifreler eşleşmiyor.";
                return;
            }

            var user = new User
            {
                FullName = fullNameTextBox.Text.Trim(),
                Email = emailTextBox.Text.Trim(),
                SchoolNumber = schoolNumberTextBox.Text.Trim(),
                Phone = phoneTextBox.Text.Trim(),
                Role = RoleConstants.Student
            };

            var validation = userService.Register(user, passwordTextBox.Text);
            if (!validation.IsValid)
            {
                messageLabel.Text = string.Join(Environment.NewLine, validation.Errors);
                return;
            }

            MessageBox.Show("Kayıt tamamlandı. Giriş ekranına dönebilirsiniz.", "Başarılı", MessageBoxButtons.OK, MessageBoxIcon.Information);
            Close();
        }
    }
}
