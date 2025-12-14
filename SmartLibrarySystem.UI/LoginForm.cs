using System;
using System.Drawing;
using System.Windows.Forms;
using SmartLibrarySystem.BLL;
using SmartLibrarySystem.Models;

namespace SmartLibrarySystem.UI
{
    public class LoginForm : Form
    {
        private readonly UserService userService = new UserService();
        private readonly TextBox emailTextBox = new TextBox();
        private readonly TextBox passwordTextBox = new TextBox();
        private readonly Label messageLabel = new Label();

        public LoginForm()
        {
            InitializeComponent();
        }

        private void InitializeComponent()
        {
            Text = "SmartLibrarySystem - Giriş";
            Size = new Size(420, 320);
            StartPosition = FormStartPosition.CenterScreen;
            FormBorderStyle = FormBorderStyle.FixedDialog;
            MaximizeBox = false;

            var titleLabel = new Label
            {
                Text = "Akıllı Kütüphane Sistemi",
                AutoSize = true,
                Font = new Font("Segoe UI", 14, FontStyle.Bold),
                Dock = DockStyle.Top,
                TextAlign = ContentAlignment.MiddleCenter,
                Padding = new Padding(0, 10, 0, 10)
            };

            var table = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                ColumnCount = 2,
                RowCount = 4,
                Padding = new Padding(20),
                AutoSize = true
            };
            table.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 35));
            table.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 65));

            var emailLabel = new Label { Text = "Email:", Dock = DockStyle.Fill, TextAlign = ContentAlignment.MiddleLeft };
            var passwordLabel = new Label { Text = "Şifre:", Dock = DockStyle.Fill, TextAlign = ContentAlignment.MiddleLeft };

            passwordTextBox.PasswordChar = '*';

            var loginButton = new Button
            {
                Text = "Giriş",
                Dock = DockStyle.Fill
            };
            loginButton.Click += LoginButton_Click;

            var registerButton = new Button
            {
                Text = "Kayıt Ol",
                Dock = DockStyle.Fill
            };
            registerButton.Click += RegisterButton_Click;

            messageLabel.ForeColor = Color.Red;
            messageLabel.AutoSize = true;
            messageLabel.Dock = DockStyle.Fill;

            table.Controls.Add(emailLabel, 0, 0);
            table.Controls.Add(emailTextBox, 1, 0);
            table.Controls.Add(passwordLabel, 0, 1);
            table.Controls.Add(passwordTextBox, 1, 1);
            table.Controls.Add(loginButton, 0, 2);
            table.Controls.Add(registerButton, 1, 2);
            table.Controls.Add(messageLabel, 0, 3);
            table.SetColumnSpan(messageLabel, 2);

            Controls.Add(table);
            Controls.Add(titleLabel);

            AcceptButton = loginButton;
        }

        private void LoginButton_Click(object sender, EventArgs e)
        {
            messageLabel.Text = string.Empty;

            var email = emailTextBox.Text.Trim();
            var password = passwordTextBox.Text;

            var user = userService.Login(email, password);
            if (user == null)
            {
                messageLabel.Text = "Email veya şifre hatalı.";
                return;
            }

            OpenDashboard(user);
        }

        private void RegisterButton_Click(object sender, EventArgs e)
        {
            using (var register = new RegistrationForm())
            {
                register.ShowDialog(this);
            }
        }

        private void OpenDashboard(User user)
        {
            Form dashboard;
            if (user.Role == RoleConstants.Student)
            {
                dashboard = new StudentDashboard(user);
            }
            else if (user.Role == RoleConstants.Staff)
            {
                dashboard = new StaffDashboard(user);
            }
            else
            {
                dashboard = new AdminDashboard(user);
            }

            dashboard.FormClosed += (_, __) => Close();
            Hide();
            dashboard.Show();
        }
    }
}
