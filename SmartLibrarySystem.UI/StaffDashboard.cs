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
        private Button logoutButton;
        private Panel statsCanvas;
        private Label cardPending;
        private Label cardApproved;
        private Label cardDelivered;
        private Label cardReturned;
        private Label cardOverdue;
        private Label statusLabel;
        private int loadingDepth;
        private DateTime loadingStartedAt;
        private Timer loadingTimer;
        private const string DateTimeDisplayFormat = "dd.MM.yyyy HH:mm";
        private const string OverdueFilterValue = "OVERDUE";
        private FlowLayoutPanel statusTimeline;

        private int pendingCount;
        private int approvedCount;
        private int deliveredCount;
        private int returnedCount;
        private int overdueCount;
        private List<BorrowRequest> allRequests = new List<BorrowRequest>();
        private List<BorrowRequest> filteredRequests = new List<BorrowRequest>();

        private class StatusOption
        {
            public string Text { get; set; }
            public string Value { get; set; }
            public override string ToString() => Text;
        }

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
                AutoGenerateColumns = false,
                SelectionMode = DataGridViewSelectionMode.FullRowSelect,
                AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill,
                AllowUserToAddRows = false,
                AllowUserToDeleteRows = false,
                MultiSelect = false
            };
            requestsGrid.Columns.Add(new DataGridViewTextBoxColumn { DataPropertyName = nameof(BorrowRequest.RequestId), HeaderText = "Talep No", Width = 70 });
            requestsGrid.Columns.Add(new DataGridViewTextBoxColumn { DataPropertyName = nameof(BorrowRequest.UserName), HeaderText = "Kullanıcı" });
            requestsGrid.Columns.Add(new DataGridViewTextBoxColumn { DataPropertyName = nameof(BorrowRequest.BookTitle), HeaderText = "Kitap" });
            requestsGrid.Columns.Add(new DataGridViewTextBoxColumn { DataPropertyName = nameof(BorrowRequest.BookAuthor), HeaderText = "Yazar" });
            requestsGrid.Columns.Add(new DataGridViewTextBoxColumn { DataPropertyName = nameof(BorrowRequest.Status), HeaderText = "Durum", Width = 110 });
            requestsGrid.Columns.Add(new DataGridViewTextBoxColumn { DataPropertyName = nameof(BorrowRequest.RequestDate), HeaderText = "Talep Tarihi", Width = 130 });
            requestsGrid.Columns.Add(new DataGridViewTextBoxColumn { DataPropertyName = nameof(BorrowRequest.DeliveryDate), HeaderText = "Teslim Tarihi", Width = 130 });
            requestsGrid.Columns.Add(new DataGridViewTextBoxColumn { DataPropertyName = nameof(BorrowRequest.ReturnDate), HeaderText = "İade Tarihi", Width = 130 });
            requestsGrid.SelectionChanged += (_, __) => UpdateActionButtons();
            requestsGrid.RowPrePaint += RequestsGrid_RowPrePaint;
            requestsGrid.CellFormatting += RequestsGrid_CellFormatting;

            var filterPanel = new FlowLayoutPanel
            {
                Dock = DockStyle.Top,
                FlowDirection = FlowDirection.LeftToRight,
                Padding = new Padding(10, 8, 10, 0),
                AutoSize = true,
                AutoSizeMode = AutoSizeMode.GrowAndShrink
            };
            filterPanel.Controls.Add(new Label { Text = "Durum:", AutoSize = true, Margin = new Padding(0, 6, 4, 0) });
            cmbStatusFilter = new ComboBox { DropDownStyle = ComboBoxStyle.DropDownList, Width = 180 };
            cmbStatusFilter.DisplayMember = nameof(StatusOption.Text);
            cmbStatusFilter.ValueMember = nameof(StatusOption.Value);
            cmbStatusFilter.Items.Add(new StatusOption { Text = "Tümü", Value = null });
            cmbStatusFilter.Items.Add(new StatusOption { Text = RequestStatus.ToDisplay(RequestStatus.Pending), Value = RequestStatus.Pending });
            cmbStatusFilter.Items.Add(new StatusOption { Text = RequestStatus.ToDisplay(RequestStatus.Approved), Value = RequestStatus.Approved });
            cmbStatusFilter.Items.Add(new StatusOption { Text = RequestStatus.ToDisplay(RequestStatus.Delivered), Value = RequestStatus.Delivered });
            cmbStatusFilter.Items.Add(new StatusOption { Text = RequestStatus.ToDisplay(RequestStatus.Returned), Value = RequestStatus.Returned });
            cmbStatusFilter.Items.Add(new StatusOption { Text = "Geciken", Value = OverdueFilterValue });
            cmbStatusFilter.SelectedIndex = 0;
            var btnApplyFilter = new Button { Text = "Filtrele", AutoSize = true, Margin = new Padding(8, 4, 0, 0) };
            btnApplyFilter.Click += (_, __) => ApplyRequestFilter();
            var btnClearFilter = new Button { Text = "Filtreyi Temizle", AutoSize = true, Margin = new Padding(6, 4, 0, 0) };
            btnClearFilter.Click += (_, __) =>
            {
                cmbStatusFilter.SelectedIndex = 0;
                ApplyRequestFilter();
            };
            var btnQuickPending = new Button { Text = "Sadece Bekleyen", AutoSize = true, Margin = new Padding(6, 4, 0, 0) };
            btnQuickPending.Click += (_, __) =>
            {
                SelectStatusFilterValue(RequestStatus.Pending);
                ApplyRequestFilter();
            };
            var btnQuickOverdue = new Button { Text = "Sadece Geciken", AutoSize = true, Margin = new Padding(6, 4, 0, 0) };
            btnQuickOverdue.Click += (_, __) =>
            {
                SelectStatusFilterValue(OverdueFilterValue);
                ApplyRequestFilter();
            };
            filterPanel.Controls.Add(cmbStatusFilter);
            filterPanel.Controls.Add(btnApplyFilter);
            filterPanel.Controls.Add(btnClearFilter);
            filterPanel.Controls.Add(btnQuickPending);
            filterPanel.Controls.Add(btnQuickOverdue);

            var actionPanel = new FlowLayoutPanel
            {
                Dock = DockStyle.Bottom,
                FlowDirection = FlowDirection.LeftToRight,
                Height = 45,
                Padding = new Padding(10)
            };

            approveButton = new Button { Text = "✅ Onayla", Width = 120 };
            approveButton.Click += (_, __) => ApplyStatus(RequestStatus.Approved);

            deliverButton = new Button { Text = "📦 Teslim Edildi", Width = 140 };
            deliverButton.Click += (_, __) => ApplyStatus(RequestStatus.Delivered);

            returnButton = new Button { Text = "↩️ İade Alındı", Width = 140 };
            returnButton.Click += (_, __) => ApplyStatus(RequestStatus.Returned);

            var refreshButton = new Button { Text = "♻️ Yenile", Width = 100 };
            refreshButton.Click += (_, __) =>
            {
                LoadRequests();
                LoadSummary();
            };
            var clearSelectionButton = new Button { Text = "🧹 Seçimi Temizle", Width = 140 };
            clearSelectionButton.Click += (_, __) =>
            {
                requestsGrid.ClearSelection();
                UpdateActionButtons();
            };

            actionPanel.Controls.Add(approveButton);
            actionPanel.Controls.Add(deliverButton);
            actionPanel.Controls.Add(returnButton);
            actionPanel.Controls.Add(refreshButton);
            actionPanel.Controls.Add(clearSelectionButton);

            statusTimeline = new FlowLayoutPanel
            {
                Dock = DockStyle.Top,
                Height = 36,
                FlowDirection = FlowDirection.LeftToRight,
                Padding = new Padding(10, 6, 0, 6),
                AutoSize = false
            };
            BuildStatusTimeline();

            var requestsContainer = new Panel { Dock = DockStyle.Fill };
            requestsContainer.Controls.Add(requestsGrid);
            requestsContainer.Controls.Add(statusTimeline);
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
                WrapContents = true,
                AutoScroll = true,
                Padding = new Padding(0, 4, 0, 0)
            };

            Panel StatCard(string title, out Label valueLabel, Color color)
            {
                var panel = new Panel
                {
                    Width = 170,
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

            var container = new Panel { Dock = DockStyle.Fill };
            container.Controls.Add(mainLayout);
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

        private void LoadRequests()
        {
            SetLoading(true);
            try
            {
                allRequests = requestService.GetRequests().ToList();
                ApplyRequestFilter();
                UpdateActionButtons();
                UpdateStatusTimeline(GetSelectedRequest());
            }
            finally
            {
                SetLoading(false);
            }
        }

        private void LoadSummary()
        {
            SetLoading(true);
            try
            {
                // Günlük dağılım (bugün oluşturulan talepler)
                var today = DateTime.Today;
                var todayRequests = allRequests.Where(r => r.RequestDate.Date == today).ToList();

                overdueCount = todayRequests.Count(IsOverdue);
                pendingCount = todayRequests.Count(r => r.Status == RequestStatus.Pending);
                approvedCount = todayRequests.Count(r => r.Status == RequestStatus.Approved);
                deliveredCount = todayRequests.Count(r => r.Status == RequestStatus.Delivered);
                returnedCount = todayRequests.Count(r => r.Status == RequestStatus.Returned);
                if (cardPending != null) cardPending.Text = pendingCount.ToString();
                if (cardApproved != null) cardApproved.Text = approvedCount.ToString();
                if (cardDelivered != null) cardDelivered.Text = deliveredCount.ToString();
                if (cardReturned != null) cardReturned.Text = returnedCount.ToString();
                if (cardOverdue != null) cardOverdue.Text = overdueCount.ToString();
                statsCanvas.Invalidate();
            }
            finally
            {
                SetLoading(false);
            }
        }

        private BorrowRequest GetSelectedRequest()
        {
            return requestsGrid.CurrentRow?.DataBoundItem as BorrowRequest;
        }

        private void ApplyStatus(string nextStatus)
        {
            SetLoading(true);
            approveButton.Enabled = deliverButton.Enabled = returnButton.Enabled = false;
            try
            {
                var selected = GetSelectedRequest();
                if (selected == null)
                {
                    MessageBox.Show("Lütfen bir talep seçin.", "Uyarı", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    return;
                }

                string actionText = nextStatus switch
                {
                    RequestStatus.Approved => "onaylamak",
                    RequestStatus.Delivered => "teslim edildi olarak işaretlemek",
                    RequestStatus.Returned => "iade alındı olarak işaretlemek",
                    _ => "güncellemek"
                };

                var confirm = MessageBox.Show(
                    $"Seçili talebi {actionText} istediğinize emin misiniz?",
                    "Onay",
                    MessageBoxButtons.YesNo,
                    MessageBoxIcon.Question,
                    MessageBoxDefaultButton.Button2);

                if (confirm != DialogResult.Yes)
                {
                    return;
                }

                var validation = requestService.UpdateStatus(selected.RequestId, nextStatus);
                if (!validation.IsValid)
                {
                    MessageBox.Show(string.Join(Environment.NewLine, validation.Errors), "Hata", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    return;
                }

                string successText = nextStatus switch
                {
                    RequestStatus.Approved => "Talep onaylandı.",
                    RequestStatus.Delivered => "Talep teslim edildi olarak işaretlendi.",
                    RequestStatus.Returned => "Talep iade alındı olarak işaretlendi.",
                    _ => "Talep güncellendi."
                };
                MessageBox.Show(successText, "Bilgi", MessageBoxButtons.OK, MessageBoxIcon.Information);

                LoadRequests();
                LoadSummary();
                UpdateActionButtons();
            }
            finally
            {
                SetLoading(false);
            }
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
            UpdateStatusTimeline(selected);
        }

        private void BuildStatusTimeline()
        {
            statusTimeline.Controls.Clear();
            AddStatusBadge(RequestStatus.Pending, "Beklemede", Color.FromArgb(90, 155, 212));
            AddStatusBadge(RequestStatus.Approved, "Onaylandı", Color.FromArgb(155, 200, 100));
            AddStatusBadge(RequestStatus.Delivered, "Teslim Edildi", Color.FromArgb(246, 153, 63));
            AddStatusBadge(RequestStatus.Returned, "İade Edildi", Color.FromArgb(120, 120, 120));
        }

        private void AddStatusBadge(string status, string text, Color color)
        {
            var lbl = new Label
            {
                Name = $"badge_{status}",
                Text = text,
                AutoSize = true,
                ForeColor = Color.White,
                BackColor = color,
                Padding = new Padding(8, 4, 8, 4),
                Margin = new Padding(0, 0, 8, 0),
                BorderStyle = BorderStyle.FixedSingle,
                Tag = color
            };
            statusTimeline.Controls.Add(lbl);
        }

        private void UpdateStatusTimeline(BorrowRequest request)
        {
            foreach (Control control in statusTimeline.Controls)
            {
                if (control is Label lbl)
                {
                    var baseColor = (Color)(lbl.Tag ?? lbl.BackColor);
                    var active = request != null && lbl.Name.EndsWith(request.Status, StringComparison.OrdinalIgnoreCase);
                    lbl.BackColor = active ? ControlPaint.Dark(baseColor) : baseColor;
                    lbl.Font = new Font(lbl.Font, active ? FontStyle.Bold : FontStyle.Regular);
                }
            }
        }

        private void SelectStatusFilterValue(string value)
        {
            var option = cmbStatusFilter.Items.Cast<StatusOption>().FirstOrDefault(o => o.Value == value);
            if (option != null)
            {
                cmbStatusFilter.SelectedItem = option;
            }
        }

        private void ApplyRequestFilter()
        {
            SetLoading(true);
            try
            {
                IEnumerable<BorrowRequest> filtered = allRequests;
                var selected = cmbStatusFilter.SelectedItem as StatusOption;
                var value = selected?.Value;
                if (!string.IsNullOrWhiteSpace(value))
                {
                    if (value == OverdueFilterValue)
                    {
                        filtered = allRequests.Where(IsOverdue);
                    }
                    else
                    {
                        filtered = allRequests.Where(r => r.Status == value);
                    }
                }

                filteredRequests = filtered.ToList();
                requestsGrid.DataSource = new BindingSource { DataSource = filteredRequests };
                LoadSummary(); // grafik/kartlar genel toplamı gösterir
            }
            finally
            {
                SetLoading(false);
            }
        }

        private bool IsOverdue(BorrowRequest request)
        {
            if (request.Status != RequestStatus.Delivered) return false;
            if (!request.DeliveryDate.HasValue) return false;
            if (request.ReturnDate.HasValue) return false;
            return request.DeliveryDate.Value.AddDays(14) < DateTime.Now;
        }

        private void RequestsGrid_CellFormatting(object sender, DataGridViewCellFormattingEventArgs e)
        {
            var property = requestsGrid.Columns[e.ColumnIndex].DataPropertyName;
            if (property == nameof(BorrowRequest.Status) && e.Value is string status)
            {
                e.Value = RequestStatus.ToDisplay(status);
                var cell = requestsGrid.Rows[e.RowIndex].Cells[e.ColumnIndex];
                cell.Style.ForeColor = status switch
                {
                    RequestStatus.Pending => Color.FromArgb(90, 155, 212),
                    RequestStatus.Approved => Color.FromArgb(155, 200, 100),
                    RequestStatus.Delivered => Color.FromArgb(246, 153, 63),
                    RequestStatus.Returned => Color.FromArgb(120, 120, 120),
                    _ => Color.DimGray
                };
                e.FormattingApplied = true;
                return;
            }

            if (e.Value is DateTime dt)
            {
                e.Value = dt.ToString(DateTimeDisplayFormat);
                e.FormattingApplied = true;
            }
        }

        private void RequestsGrid_RowPrePaint(object sender, DataGridViewRowPrePaintEventArgs e)
        {
            if (requestsGrid.Rows[e.RowIndex].DataBoundItem is BorrowRequest req)
            {
                if (IsOverdue(req))
                {
                    requestsGrid.Rows[e.RowIndex].DefaultCellStyle.BackColor = Color.MistyRose;
                    requestsGrid.Rows[e.RowIndex].DefaultCellStyle.SelectionBackColor = Color.LightCoral;
                }
                else
                {
                    requestsGrid.Rows[e.RowIndex].DefaultCellStyle.BackColor = Color.White;
                    requestsGrid.Rows[e.RowIndex].DefaultCellStyle.SelectionBackColor = Color.LightGray;
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
