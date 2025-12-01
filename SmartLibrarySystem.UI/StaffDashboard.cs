using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;
using SmartLibrarySystem.BLL;
using SmartLibrarySystem.Models;

namespace SmartLibrarySystem.UI
{
    public class StaffDashboard : Form
    {
        private readonly User currentUser;
        private readonly RequestService requestService = new RequestService();

        private DataGridView requestsGrid;
        private DataGridView overdueGrid;
        private Label borrowCountLabel;
        private Label returnCountLabel;
        private Label overdueCountLabel;
        private Button approveButton;
        private Button deliverButton;
        private Button returnButton;

        public StaffDashboard(User user)
        {
            currentUser = user;
            InitializeComponent();
            LoadRequests();
            LoadOverdue();
            LoadSummary();
        }

        private void InitializeComponent()
        {
            Text = $"Personel Paneli - {currentUser.FullName}";
            WindowState = FormWindowState.Maximized;

            var mainLayout = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                RowCount = 2,
                ColumnCount = 2
            };
            mainLayout.RowStyles.Add(new RowStyle(SizeType.Percent, 70));
            mainLayout.RowStyles.Add(new RowStyle(SizeType.Percent, 30));
            mainLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 70));
            mainLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 30));

            requestsGrid = new DataGridView
            {
                Dock = DockStyle.Fill,
                ReadOnly = true,
                AutoGenerateColumns = true,
                SelectionMode = DataGridViewSelectionMode.FullRowSelect
            };
            requestsGrid.SelectionChanged += (_, __) => UpdateActionButtons();

            var actionPanel = new FlowLayoutPanel
            {
                Dock = DockStyle.Bottom,
                FlowDirection = FlowDirection.LeftToRight,
                Height = 45,
                Padding = new Padding(10)
            };

            approveButton = new Button { Text = "Onayla", Width = 100 };
            approveButton.Click += (_, __) => ApplyStatus(RequestStatus.Approved);

            deliverButton = new Button { Text = "Teslim Edildi", Width = 120 };
            deliverButton.Click += (_, __) => ApplyStatus(RequestStatus.Delivered);

            returnButton = new Button { Text = "İade Alındı", Width = 120 };
            returnButton.Click += (_, __) => ApplyStatus(RequestStatus.Returned);

            var refreshButton = new Button { Text = "Yenile", Width = 80 };
            refreshButton.Click += (_, __) =>
            {
                LoadRequests();
                LoadOverdue();
                LoadSummary();
            };

            actionPanel.Controls.Add(approveButton);
            actionPanel.Controls.Add(deliverButton);
            actionPanel.Controls.Add(returnButton);
            actionPanel.Controls.Add(refreshButton);

            var requestsContainer = new Panel { Dock = DockStyle.Fill };
            requestsContainer.Controls.Add(requestsGrid);
            requestsContainer.Controls.Add(actionPanel);

            var summaryPanel = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                RowCount = 2,
                ColumnCount = 1
            };
            summaryPanel.RowStyles.Add(new RowStyle(SizeType.Percent, 40));
            summaryPanel.RowStyles.Add(new RowStyle(SizeType.Percent, 60));

            var statsPanel = new FlowLayoutPanel
            {
                Dock = DockStyle.Fill,
                FlowDirection = FlowDirection.TopDown,
                Padding = new Padding(10)
            };
            borrowCountLabel = new Label { Font = new Font("Segoe UI", 11, FontStyle.Bold), AutoSize = true };
            returnCountLabel = new Label { Font = new Font("Segoe UI", 11, FontStyle.Bold), AutoSize = true };
            overdueCountLabel = new Label { Font = new Font("Segoe UI", 11, FontStyle.Bold), AutoSize = true };
            statsPanel.Controls.Add(borrowCountLabel);
            statsPanel.Controls.Add(returnCountLabel);
            statsPanel.Controls.Add(overdueCountLabel);

            overdueGrid = new DataGridView
            {
                Dock = DockStyle.Fill,
                ReadOnly = true,
                AutoGenerateColumns = true,
                SelectionMode = DataGridViewSelectionMode.FullRowSelect
            };

            summaryPanel.Controls.Add(statsPanel, 0, 0);
            summaryPanel.Controls.Add(overdueGrid, 0, 1);

            mainLayout.Controls.Add(requestsContainer, 0, 0);
            mainLayout.SetRowSpan(requestsContainer, 2);
            mainLayout.Controls.Add(summaryPanel, 1, 0);
            mainLayout.SetRowSpan(summaryPanel, 2);

            Controls.Add(mainLayout);
        }

        private void LoadRequests()
        {
            var requests = requestService.GetRequests();
            requestsGrid.DataSource = new BindingSource { DataSource = new List<BorrowRequest>(requests) };
            UpdateActionButtons();
        }

        private void LoadOverdue()
        {
            var overdue = requestService.GetOverdue();
            overdueGrid.DataSource = new BindingSource { DataSource = new List<BorrowRequest>(overdue) };
        }

        private void LoadSummary()
        {
            var today = DateTime.Today;
            borrowCountLabel.Text = $"Bugün verilen kitap: {requestService.GetDailyBorrowCount(today)}";
            returnCountLabel.Text = $"Bugün iade edilen: {requestService.GetDailyReturnCount(today)}";
            overdueCountLabel.Text = $"Geciken kitaplar: {requestService.GetOverdue().Count()}";
        }

        private BorrowRequest GetSelectedRequest()
        {
            return requestsGrid.CurrentRow?.DataBoundItem as BorrowRequest;
        }

        private void ApplyStatus(string nextStatus)
        {
            var selected = GetSelectedRequest();
            if (selected == null)
            {
                MessageBox.Show("Lütfen bir talep seçin.", "Uyarı", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            var validation = requestService.UpdateStatus(selected.RequestId, nextStatus);
            if (!validation.IsValid)
            {
                MessageBox.Show(string.Join(Environment.NewLine, validation.Errors), "Hata", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return;
            }

            LoadRequests();
            LoadOverdue();
            LoadSummary();
        }

        private void UpdateActionButtons()
        {
            var selected = GetSelectedRequest();
            approveButton.Enabled = false;
            deliverButton.Enabled = false;
            returnButton.Enabled = false;

            if (selected == null) return;

            if (selected.Status == RequestStatus.Pending)
            {
                approveButton.Enabled = true;
            }
            else if (selected.Status == RequestStatus.Approved)
            {
                deliverButton.Enabled = true;
            }
            else if (selected.Status == RequestStatus.Delivered)
            {
                returnButton.Enabled = true;
            }
        }
    }
}
