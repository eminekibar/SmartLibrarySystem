using System;
using System.Drawing;
using System.Windows.Forms;
using System.Linq;
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
            var formPadding = 20;
            Padding = new Padding(formPadding);

            passwordTextBox.PasswordChar = '*';
            confirmPasswordTextBox.PasswordChar = '*';

            var table = new TableLayoutPanel
            {
                ColumnCount = 2,
                RowCount = 7,
                Padding = new Padding(10),
                AutoSize = true,
                AutoSizeMode = AutoSizeMode.GrowAndShrink,
                Anchor = AnchorStyles.None
            };
            table.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 35));
            table.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 65));

            table.Controls.Add(new Label { Text = "Ad Soyad:", TextAlign = ContentAlignment.MiddleLeft, Dock = DockStyle.Fill }, 0, 0);
            table.Controls.Add(fullNameTextBox, 1, 0);
            table.Controls.Add(new Label { Text = "E-posta:", TextAlign = ContentAlignment.MiddleLeft, Dock = DockStyle.Fill }, 0, 1);
            table.Controls.Add(emailTextBox, 1, 1);
            table.Controls.Add(new Label { Text = "Okul No:", TextAlign = ContentAlignment.MiddleLeft, Dock = DockStyle.Fill }, 0, 2);
            table.Controls.Add(schoolNumberTextBox, 1, 2);
            table.Controls.Add(new Label { Text = "Telefon:", TextAlign = ContentAlignment.MiddleLeft, Dock = DockStyle.Fill }, 0, 3);
            phoneTextBox.KeyPress += DigitOnly_KeyPress;
            table.Controls.Add(phoneTextBox, 1, 3);
            table.Controls.Add(new Label { Text = "Şifre:", TextAlign = ContentAlignment.MiddleLeft, Dock = DockStyle.Fill }, 0, 4);
            table.Controls.Add(passwordTextBox, 1, 4);
            table.Controls.Add(new Label { Text = "Şifre Tekrar:", TextAlign = ContentAlignment.MiddleLeft, Dock = DockStyle.Fill }, 0, 5);
            table.Controls.Add(confirmPasswordTextBox, 1, 5);

            var registerButton = new Button
            {
                Text = "Kaydol",
                AutoSize = true,
                Anchor = AnchorStyles.None,
                Padding = new Padding(12, 8, 12, 8)
            };
            registerButton.Click += RegisterButton_Click;
            var cancelButton = new Button
            {
                Text = "İptal",
                AutoSize = true,
                Anchor = AnchorStyles.None,
                Padding = new Padding(12, 8, 12, 8),
                Margin = new Padding(10, 0, 0, 0)
            };
            cancelButton.Click += (_, __) => Close();

            var buttonsPanel = new FlowLayoutPanel
            {
                Dock = DockStyle.Fill,
                FlowDirection = FlowDirection.LeftToRight,
                AutoSize = true,
                AutoSizeMode = AutoSizeMode.GrowAndShrink,
                Anchor = AnchorStyles.None
            };
            buttonsPanel.Controls.Add(registerButton);
            buttonsPanel.Controls.Add(cancelButton);

            table.Controls.Add(buttonsPanel, 0, 6);
            table.SetColumnSpan(buttonsPanel, 2);

            messageLabel.ForeColor = Color.Red;
            messageLabel.AutoSize = true;
            messageLabel.Dock = DockStyle.Bottom;

            var container = new Panel { Dock = DockStyle.Fill };
            container.Controls.Add(table);
            container.Resize += (_, __) =>
            {
                // Keep the form contents centered as the dialog size changes.
                table.Location = new Point(
                    Math.Max((container.ClientSize.Width - table.Width) / 2, 0),
                    Math.Max((container.ClientSize.Height - table.Height) / 2, 0));
            };

            Controls.Add(container);
            Controls.Add(messageLabel);

            AcceptButton = registerButton;
        }

        private void RegisterButton_Click(object sender, EventArgs e)
        {
            messageLabel.Text = string.Empty;

            if (!string.Equals(passwordTextBox.Text, confirmPasswordTextBox.Text))
            {
                MessageBox.Show("Şifreler eşleşmiyor.", "Uyarı", MessageBoxButtons.OK, MessageBoxIcon.Warning);
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
                var priorityOrder = new[]
                {
                    "Ad soyad boş",
                    "Okul numarası boş",
                    "E-posta boş",
                    "E-posta formatı",
                    "Parola boş",
                    "Parola en az",
                    "Bu e-posta ile"
                };

                int GetPriority(string error)
                {
                    for (var i = 0; i < priorityOrder.Length; i++)
                    {
                        if (error.StartsWith(priorityOrder[i], StringComparison.OrdinalIgnoreCase))
                        {
                            return i;
                        }
                    }
                    return priorityOrder.Length;
                }

                var orderedErrors = validation.Errors
                    .OrderBy(GetPriority)
                    .ThenBy(e => e)
                    .Select(e => "• " + e);

                var errorMessage = "Lütfen önce aşağıdaki hataları düzeltin:" + Environment.NewLine + Environment.NewLine + string.Join(Environment.NewLine, orderedErrors);

                MessageBox.Show(errorMessage, "Eksik veya Hatalı Bilgi", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            MessageBox.Show("Kayıt tamamlandı. Giriş ekranına dönebilirsiniz.", "Başarılı", MessageBoxButtons.OK, MessageBoxIcon.Information);
            Close();
        }

        private void DigitOnly_KeyPress(object sender, KeyPressEventArgs e)
        {
            if (!char.IsControl(e.KeyChar) && !char.IsDigit(e.KeyChar))
            {
                e.Handled = true;
            }
        }
    }
}
