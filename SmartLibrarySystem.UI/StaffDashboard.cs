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
        private ComboBox cmbStatusFilter;
        private Button approveButton;
        private Button deliverButton;
        private Button returnButton;
        private Panel statsCanvas;
        private Label cardPending;
        private Label cardApproved;
        private Label cardDelivered;
        private Label cardReturned;
        private Label cardOverdue;

        private int pendingCount;
        private int approvedCount;
        private int deliveredCount;
        private int returnedCount;
        private int overdueCount;
        private List<BorrowRequest> allRequests = new List<BorrowRequest>();
        private List<BorrowRequest> filteredRequests = new List<BorrowRequest>();

        public StaffDashboard(User user)
        {
            currentUser = user;
            InitializeComponent();
            LoadRequests();
            LoadSummary();
        }

        private void InitializeComponent()
        {
            Text = $"Personel Paneli - {currentUser.FullName}";
            WindowState = FormWindowState.Maximized;
            StartPosition = FormStartPosition.CenterScreen;

            var mainLayout = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                RowCount = 2,
                ColumnCount = 2
            };
            mainLayout.RowStyles.Add(new RowStyle(SizeType.Percent, 70));
            mainLayout.RowStyles.Add(new RowStyle(SizeType.Percent, 30));
            mainLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 65));
            mainLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 35));

            // Left: requests grid + filters + actions
            requestsGrid = new DataGridView
            {
                Dock = DockStyle.Fill,
                ReadOnly = true,
                AutoGenerateColumns = true,
                SelectionMode = DataGridViewSelectionMode.FullRowSelect
            };
            requestsGrid.SelectionChanged += (_, __) => UpdateActionButtons();
            requestsGrid.RowPrePaint += RequestsGrid_RowPrePaint;

            var filterPanel = new FlowLayoutPanel
            {
                Dock = DockStyle.Top,
                FlowDirection = FlowDirection.LeftToRight,
                Padding = new Padding(10, 8, 10, 0),
                AutoSize = true,
                AutoSizeMode = AutoSizeMode.GrowAndShrink
            };
            filterPanel.Controls.Add(new Label { Text = "Durum:", AutoSize = true, Margin = new Padding(0, 6, 4, 0) });
            cmbStatusFilter = new ComboBox { DropDownStyle = ComboBoxStyle.DropDownList, Width = 160 };
            cmbStatusFilter.Items.AddRange(new object[] { "Tümü", RequestStatus.Pending, RequestStatus.Approved, RequestStatus.Delivered, RequestStatus.Returned, "Geciken" });
            cmbStatusFilter.SelectedIndex = 0;
            var btnApplyFilter = new Button { Text = "Filtrele", AutoSize = true, Margin = new Padding(8, 4, 0, 0) };
            btnApplyFilter.Click += (_, __) => ApplyRequestFilter();
            var btnClearFilter = new Button { Text = "Filtreyi Temizle", AutoSize = true, Margin = new Padding(6, 4, 0, 0) };
            btnClearFilter.Click += (_, __) =>
            {
                cmbStatusFilter.SelectedIndex = 0;
                ApplyRequestFilter();
            };
            filterPanel.Controls.Add(cmbStatusFilter);
            filterPanel.Controls.Add(btnApplyFilter);
            filterPanel.Controls.Add(btnClearFilter);

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
                LoadSummary();
            };

            actionPanel.Controls.Add(approveButton);
            actionPanel.Controls.Add(deliverButton);
            actionPanel.Controls.Add(returnButton);
            actionPanel.Controls.Add(refreshButton);

            var requestsContainer = new Panel { Dock = DockStyle.Fill };
            requestsContainer.Controls.Add(requestsGrid);
            requestsContainer.Controls.Add(filterPanel);
            requestsContainer.Controls.Add(actionPanel);

            // Right: chart + cards
            var rightLayout = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                RowCount = 2,
                ColumnCount = 1,
                Padding = new Padding(10)
            };
            rightLayout.RowStyles.Add(new RowStyle(SizeType.Percent, 75));
            rightLayout.RowStyles.Add(new RowStyle(SizeType.Percent, 25));

            statsCanvas = new Panel { Dock = DockStyle.Fill, BackColor = Color.WhiteSmoke, Margin = new Padding(0, 0, 0, 6) };
            statsCanvas.Paint += StatsCanvas_Paint;

            var cards = new FlowLayoutPanel
            {
                Dock = DockStyle.Fill,
                FlowDirection = FlowDirection.LeftToRight,
                AutoSize = true,
                AutoSizeMode = AutoSizeMode.GrowAndShrink,
                Padding = new Padding(0, 4, 0, 0)
            };

            Panel StatCard(string title, out Label valueLabel, Color color)
            {
                var panel = new Panel
                {
                    Width = 200,
                    Height = 70,
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
                valueLabel = new Label
                {
                    Text = "0",
                    Font = new Font("Segoe UI", 16, FontStyle.Bold),
                    ForeColor = color,
                    AutoSize = true,
                    Location = new Point(12, 30)
                };
                panel.Controls.Add(titleLabel);
                panel.Controls.Add(valueLabel);
                return panel;
            }

            var card1 = StatCard("Beklemede", out cardPending, Color.FromArgb(90, 155, 212));
            var card2 = StatCard("Onaylandı", out cardApproved, Color.FromArgb(155, 200, 100));
            var card3 = StatCard("Teslim Edildi", out cardDelivered, Color.FromArgb(246, 153, 63));
            var card4 = StatCard("İade Edildi", out cardReturned, Color.FromArgb(120, 120, 120));
            var card5 = StatCard("Geciken", out cardOverdue, Color.FromArgb(200, 80, 80));

            cards.Controls.Add(card1);
            cards.Controls.Add(card2);
            cards.Controls.Add(card3);
            cards.Controls.Add(card4);
            cards.Controls.Add(card5);

            rightLayout.Controls.Add(statsCanvas, 0, 0);
            rightLayout.Controls.Add(cards, 0, 1);

            mainLayout.Controls.Add(requestsContainer, 0, 0);
            mainLayout.SetRowSpan(requestsContainer, 2);
            mainLayout.Controls.Add(rightLayout, 1, 0);
            mainLayout.SetRowSpan(rightLayout, 2);

            Controls.Add(mainLayout);
        }

        private void LoadRequests()
        {
            allRequests = requestService.GetRequests().ToList();
            ApplyRequestFilter();
            UpdateActionButtons();
        }

        private void LoadSummary()
        {
            // Genel dağılım (grafik ve kartlar)
            overdueCount = allRequests.Count(IsOverdue);
            pendingCount = allRequests.Count(r => r.Status == RequestStatus.Pending);
            approvedCount = allRequests.Count(r => r.Status == RequestStatus.Approved);
            deliveredCount = allRequests.Count(r => r.Status == RequestStatus.Delivered);
            returnedCount = allRequests.Count(r => r.Status == RequestStatus.Returned);
            if (cardPending != null) cardPending.Text = pendingCount.ToString();
            if (cardApproved != null) cardApproved.Text = approvedCount.ToString();
            if (cardDelivered != null) cardDelivered.Text = deliveredCount.ToString();
            if (cardReturned != null) cardReturned.Text = returnedCount.ToString();
            if (cardOverdue != null) cardOverdue.Text = overdueCount.ToString();
            statsCanvas.Invalidate();
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

        private void ApplyRequestFilter()
        {
            IEnumerable<BorrowRequest> filtered = allRequests;
            var selected = cmbStatusFilter.SelectedItem?.ToString();
            if (!string.IsNullOrWhiteSpace(selected) && selected != "Tümü")
            {
                if (selected == "Geciken")
                {
                    filtered = allRequests.Where(IsOverdue);
                }
                else
                {
                    filtered = allRequests.Where(r => r.Status == selected);
                }
            }

            filteredRequests = filtered.ToList();
            requestsGrid.DataSource = new BindingSource { DataSource = filteredRequests };
            LoadSummary(); // grafik/kartlar genel toplamı gösterir
        }

        private bool IsOverdue(BorrowRequest request)
        {
            if (request.Status != RequestStatus.Delivered) return false;
            if (!request.DeliveryDate.HasValue) return false;
            if (request.ReturnDate.HasValue) return false;
            return request.DeliveryDate.Value.AddDays(14) < DateTime.Now;
        }

        private void RequestsGrid_RowPrePaint(object sender, DataGridViewRowPrePaintEventArgs e)
        {
            if (requestsGrid.Rows[e.RowIndex].DataBoundItem is BorrowRequest req)
            {
                if (IsOverdue(req))
                {
                    requestsGrid.Rows[e.RowIndex].DefaultCellStyle.BackColor = Color.MistyRose;
                }
                else
                {
                    requestsGrid.Rows[e.RowIndex].DefaultCellStyle.BackColor = Color.White;
                }
            }
        }

        private void StatsCanvas_Paint(object sender, PaintEventArgs e)
        {
            var g = e.Graphics;
            g.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.AntiAlias;
            g.Clear(Color.WhiteSmoke);

            var counts = new List<(string Label, int Value, Color Color)>
            {
                ("Beklemede", pendingCount, Color.FromArgb(90,155,212)),
                ("Onaylandı", approvedCount, Color.FromArgb(155,200,100)),
                ("Teslim Edildi", deliveredCount, Color.FromArgb(246,153,63)),
                ("İade Edildi", returnedCount, Color.FromArgb(120,120,120))
            };

            int total = counts.Sum(c => c.Value);
            const int padding = 20;
            const int legendLines = 4;
            int legendHeight = legendLines * 22 + 5;
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
            foreach (var c in counts)
            {
                if (c.Value <= 0) continue;
                float sweep = 360f * c.Value / total;
                using (var brush = new SolidBrush(c.Color))
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
            for (int i = 0; i < counts.Count; i++)
            {
                var c = counts[i];
                var box = new Rectangle(legendX, legendY + i * 22, 14, 14);
                using (var brush = new SolidBrush(c.Color))
                {
                    g.FillRectangle(brush, box);
                }
                g.DrawRectangle(Pens.Gray, box);
                g.DrawString(c.Label, new Font(Font, FontStyle.Bold), Brushes.Black, box.Right + 6, box.Top - 1);
            }
        }
    }
}
