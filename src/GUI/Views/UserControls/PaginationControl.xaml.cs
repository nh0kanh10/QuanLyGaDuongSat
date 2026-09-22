using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

namespace GUI.Views.UserControls
{
    public partial class PaginationControl : UserControl
    {
        public event EventHandler<int>? PageChanged;
        public event EventHandler<int>? PageSizeChanged;

        private bool _isUpdatingUi = false;

        public int CurrentPage { get; private set; } = 1;
        public int TotalPages { get; private set; } = 1;
        public int TotalItems { get; private set; } = 0;
        public int PageSize { get; private set; } = 50;

        public PaginationControl()
        {
            InitializeComponent();
        }

        public void SetPagingInfo(int totalItems, int currentPage, int pageSize, string unitName = "ga")
        {
            _isUpdatingUi = true;
            try
            {
                TotalItems = totalItems;
                PageSize = pageSize;

                if (pageSize <= 0) // Tất cả
                {
                    TotalPages = 1;
                    CurrentPage = 1;
                }
                else
                {
                    TotalPages = Math.Max(1, (int)Math.Ceiling((double)totalItems / pageSize));
                    CurrentPage = Math.Clamp(currentPage, 1, TotalPages);
                }

                txtCurrentPage.Text = CurrentPage.ToString();
                txtTotalPages.Text = $"/ {TotalPages}";

                // Hiển thị tóm tắt số lượng
                if (totalItems == 0)
                {
                    txtSummary.Text = $"Không có dữ liệu {unitName}";
                }
                else if (pageSize <= 0 || pageSize >= totalItems)
                {
                    txtSummary.Text = $"Hiển thị {totalItems} / {totalItems} {unitName}";
                }
                else
                {
                    int from = (CurrentPage - 1) * pageSize + 1;
                    int to = Math.Min(CurrentPage * pageSize, totalItems);
                    txtSummary.Text = $"Hiển thị {from} - {to} / {totalItems} {unitName}";
                }

                // Trạng thái các nút điều hướng
                btnFirst.IsEnabled = CurrentPage > 1;
                btnPrev.IsEnabled = CurrentPage > 1;
                btnNext.IsEnabled = CurrentPage < TotalPages;
                btnLast.IsEnabled = CurrentPage < TotalPages;

                btnFirst.Opacity = btnFirst.IsEnabled ? 1.0 : 0.4;
                btnPrev.Opacity = btnPrev.IsEnabled ? 1.0 : 0.4;
                btnNext.Opacity = btnNext.IsEnabled ? 1.0 : 0.4;
                btnLast.Opacity = btnLast.IsEnabled ? 1.0 : 0.4;
            }
            finally
            {
                _isUpdatingUi = false;
            }
        }

        private void GoToPage(int page)
        {
            page = Math.Clamp(page, 1, TotalPages);
            if (page != CurrentPage)
            {
                CurrentPage = page;
                PageChanged?.Invoke(this, CurrentPage);
            }
        }

        private void BtnFirst_Click(object sender, RoutedEventArgs e) => GoToPage(1);
        private void BtnPrev_Click(object sender, RoutedEventArgs e) => GoToPage(CurrentPage - 1);
        private void BtnNext_Click(object sender, RoutedEventArgs e) => GoToPage(CurrentPage + 1);
        private void BtnLast_Click(object sender, RoutedEventArgs e) => GoToPage(TotalPages);

        private void TxtCurrentPage_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Enter)
            {
                if (int.TryParse(txtCurrentPage.Text.Trim(), out int targetPage))
                {
                    GoToPage(targetPage);
                }
                else
                {
                    txtCurrentPage.Text = CurrentPage.ToString();
                }
            }
        }

        private void CboPageSize_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (_isUpdatingUi) return;

            if (cboPageSize.SelectedItem is ComboBoxItem item)
            {
                string content = item.Content.ToString() ?? "10";
                int newSize = content == "Tất cả" ? 0 : int.Parse(content);
                PageSize = newSize;
                PageSizeChanged?.Invoke(this, PageSize);
            }
        }
    }
}
