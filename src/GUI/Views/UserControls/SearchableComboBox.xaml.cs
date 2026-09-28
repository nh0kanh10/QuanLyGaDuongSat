using System;
using System.Collections;
using System.Collections.Generic;
using System.Data;
using System.Globalization;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

namespace GUI.Views.UserControls
{
    public class SearchableComboItem
    {
        public object RawItem { get; set; } = null!;
        public string DisplayText { get; set; } = "";
        public string NormalizedDisplayText { get; set; } = "";
        public string SubText { get; set; } = "";
        public object? Value { get; set; }
        public Visibility SubTextVisibility => string.IsNullOrEmpty(SubText) ? Visibility.Collapsed : Visibility.Visible;

        public override string ToString() => DisplayText;
    }

    public partial class SearchableComboBox : UserControl
    {
        private readonly List<SearchableComboItem> _allItems = new();
        private bool _isUpdatingInternally = false;
        private DateTime _lastClosedTime = DateTime.MinValue;
        private DateTime _popupOpenedTime = DateTime.MinValue;

        #region Dependency Properties

        public static readonly DependencyProperty ItemsSourceProperty =
            DependencyProperty.Register(nameof(ItemsSource), typeof(IEnumerable), typeof(SearchableComboBox),
                new PropertyMetadata(null, OnItemsSourceChanged));

        public static readonly DependencyProperty SelectedItemProperty =
            DependencyProperty.Register(nameof(SelectedItem), typeof(object), typeof(SearchableComboBox),
                new FrameworkPropertyMetadata(null, FrameworkPropertyMetadataOptions.BindsTwoWayByDefault, OnSelectedItemChanged));

        public static readonly DependencyProperty SelectedValueProperty =
            DependencyProperty.Register(nameof(SelectedValue), typeof(object), typeof(SearchableComboBox),
                new FrameworkPropertyMetadata(null, FrameworkPropertyMetadataOptions.BindsTwoWayByDefault, OnSelectedValueChanged));

        public static readonly DependencyProperty SelectedIndexProperty =
            DependencyProperty.Register(nameof(SelectedIndex), typeof(int), typeof(SearchableComboBox),
                new FrameworkPropertyMetadata(-1, FrameworkPropertyMetadataOptions.BindsTwoWayByDefault, OnSelectedIndexChanged));

        public static readonly DependencyProperty DisplayMemberPathProperty =
            DependencyProperty.Register(nameof(DisplayMemberPath), typeof(string), typeof(SearchableComboBox),
                new PropertyMetadata("TenGa", OnPathChanged));

        public static readonly DependencyProperty SelectedValuePathProperty =
            DependencyProperty.Register(nameof(SelectedValuePath), typeof(string), typeof(SearchableComboBox),
                new PropertyMetadata("MaGa", OnPathChanged));

        public static readonly DependencyProperty SubTextMemberPathProperty =
            DependencyProperty.Register(nameof(SubTextMemberPath), typeof(string), typeof(SearchableComboBox),
                new PropertyMetadata("LyTrinhKm", OnPathChanged));

        public static readonly DependencyProperty PlaceholderProperty =
            DependencyProperty.Register(nameof(Placeholder), typeof(string), typeof(SearchableComboBox),
                new PropertyMetadata("Chọn ga...", (d, e) => ((SearchableComboBox)d).UpdatePlaceholder()));

        public static readonly DependencyProperty IsDropDownOpenProperty =
            DependencyProperty.Register(nameof(IsDropDownOpen), typeof(bool), typeof(SearchableComboBox),
                new PropertyMetadata(false, (d, e) => ((SearchableComboBox)d).popDropdown.IsOpen = (bool)e.NewValue));

        public static readonly RoutedEvent SelectionChangedEvent =
            EventManager.RegisterRoutedEvent(nameof(SelectionChanged), RoutingStrategy.Bubble,
                typeof(SelectionChangedEventHandler), typeof(SearchableComboBox));

        #endregion

        #region CLR Properties

        public IEnumerable ItemsSource
        {
            get => (IEnumerable)GetValue(ItemsSourceProperty);
            set => SetValue(ItemsSourceProperty, value);
        }

        public object? SelectedItem
        {
            get => GetValue(SelectedItemProperty);
            set => SetValue(SelectedItemProperty, value);
        }

        public object? SelectedValue
        {
            get => GetValue(SelectedValueProperty);
            set => SetValue(SelectedValueProperty, value);
        }

        public int SelectedIndex
        {
            get => (int)GetValue(SelectedIndexProperty);
            set => SetValue(SelectedIndexProperty, value);
        }

        public string DisplayMemberPath
        {
            get => (string)GetValue(DisplayMemberPathProperty);
            set => SetValue(DisplayMemberPathProperty, value);
        }

        public string SelectedValuePath
        {
            get => (string)GetValue(SelectedValuePathProperty);
            set => SetValue(SelectedValuePathProperty, value);
        }

        public string SubTextMemberPath
        {
            get => (string)GetValue(SubTextMemberPathProperty);
            set => SetValue(SubTextMemberPathProperty, value);
        }

        public string Placeholder
        {
            get => (string)GetValue(PlaceholderProperty);
            set => SetValue(PlaceholderProperty, value);
        }

        public bool IsDropDownOpen
        {
            get => (bool)GetValue(IsDropDownOpenProperty);
            set => SetValue(IsDropDownOpenProperty, value);
        }

        public string Text
        {
            get => txtDisplayText?.Text ?? "";
            set
            {
                if (txtDisplayText != null) txtDisplayText.Text = value;
            }
        }

        public event SelectionChangedEventHandler SelectionChanged
        {
            add => AddHandler(SelectionChangedEvent, value);
            remove => RemoveHandler(SelectionChangedEvent, value);
        }

        #endregion

        public SearchableComboBox()
        {
            InitializeComponent();
            UpdatePlaceholder();
        }

        private static void OnItemsSourceChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (d is SearchableComboBox ctrl)
            {
                ctrl.RebuildItemsList();
            }
        }

        private static void OnPathChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (d is SearchableComboBox ctrl)
            {
                ctrl.RebuildItemsList();
            }
        }

        private static void OnSelectedItemChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (d is SearchableComboBox ctrl && !ctrl._isUpdatingInternally)
            {
                ctrl.SyncFromSelectedItem(e.NewValue);
            }
        }

        private static void OnSelectedValueChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (d is SearchableComboBox ctrl && !ctrl._isUpdatingInternally)
            {
                ctrl.SyncFromSelectedValue(e.NewValue);
            }
        }

        private static void OnSelectedIndexChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (d is SearchableComboBox ctrl && !ctrl._isUpdatingInternally)
            {
                ctrl.SyncFromSelectedIndex((int)e.NewValue);
            }
        }

        private void RebuildItemsList()
        {
            _allItems.Clear();
            if (ItemsSource == null)
            {
                lstItems.ItemsSource = null;
                txtResultCount.Text = "0 mục khả dụng";
                ClearSelectionDisplay();
                return;
            }

            foreach (var item in ItemsSource)
            {
                if (item == null) continue;

                string display = ExtractProperty(item, DisplayMemberPath);
                object? val = !string.IsNullOrEmpty(SelectedValuePath) ? ExtractRawProperty(item, SelectedValuePath) : item;
                string sub = ExtractSubText(item, SubTextMemberPath);

                var comboItem = new SearchableComboItem
                {
                    RawItem = item,
                    DisplayText = display,
                    NormalizedDisplayText = RemoveDiacritics(display).ToLowerInvariant(),
                    SubText = sub,
                    Value = val
                };

                _allItems.Add(comboItem);
            }

            lstItems.ItemsSource = _allItems;
            txtResultCount.Text = $"{_allItems.Count} mục khả dụng";

            // Khôi phục hiển thị nếu đã có SelectedValue / SelectedIndex / SelectedItem
            if (SelectedValue != null)
                SyncFromSelectedValue(SelectedValue);
            else if (SelectedIndex >= 0)
                SyncFromSelectedIndex(SelectedIndex);
            else if (SelectedItem != null)
                SyncFromSelectedItem(SelectedItem);
            else
                ClearSelectionDisplay();
        }

        private string ExtractProperty(object item, string propertyPath)
        {
            if (string.IsNullOrEmpty(propertyPath)) return item.ToString() ?? "";

            if (item is DataRowView drv)
            {
                if (drv.Row.Table.Columns.Contains(propertyPath) && drv[propertyPath] != DBNull.Value)
                    return drv[propertyPath]?.ToString() ?? "";
                return "";
            }

            PropertyInfo? prop = item.GetType().GetProperty(propertyPath);
            if (prop != null)
            {
                var val = prop.GetValue(item);
                return val?.ToString() ?? "";
            }

            return item.ToString() ?? "";
        }

        private object? ExtractRawProperty(object item, string propertyPath)
        {
            if (string.IsNullOrEmpty(propertyPath)) return item;

            if (item is DataRowView drv)
            {
                if (drv.Row.Table.Columns.Contains(propertyPath) && drv[propertyPath] != DBNull.Value)
                    return drv[propertyPath];
                return null;
            }

            PropertyInfo? prop = item.GetType().GetProperty(propertyPath);
            return prop?.GetValue(item);
        }

        private string ExtractSubText(object item, string propertyPath)
        {
            if (string.IsNullOrEmpty(propertyPath)) return "";
            string raw = ExtractProperty(item, propertyPath);
            if (string.IsNullOrWhiteSpace(raw)) return "";

            if (decimal.TryParse(raw, out decimal num))
            {
                return $"km {num:N1}";
            }
            return raw;
        }

        private void SyncFromSelectedValue(object? value)
        {
            if (value == null)
            {
                ClearSelectionDisplay();
                return;
            }

            var item = _allItems.FirstOrDefault(x =>
                Equals(x.Value, value) ||
                (x.Value != null && Convert.ToString(x.Value) == Convert.ToString(value)));

            if (item != null)
            {
                ApplyItemDisplay(item);
            }
            else
            {
                ClearSelectionDisplay();
            }
        }

        private void SyncFromSelectedItem(object? rawItem)
        {
            if (rawItem == null)
            {
                ClearSelectionDisplay();
                return;
            }

            var item = _allItems.FirstOrDefault(x => ReferenceEquals(x.RawItem, rawItem) || Equals(x.RawItem, rawItem));
            if (item != null)
            {
                ApplyItemDisplay(item);
            }
        }

        private void SyncFromSelectedIndex(int index)
        {
            if (index >= 0 && index < _allItems.Count)
            {
                ApplyItemDisplay(_allItems[index]);
            }
            else
            {
                ClearSelectionDisplay();
            }
        }

        private void ApplyItemDisplay(SearchableComboItem item)
        {
            _isUpdatingInternally = true;
            try
            {
                SelectedItem = item.RawItem;
                SelectedValue = item.Value;
                SelectedIndex = _allItems.IndexOf(item);

                txtDisplayText.Text = item.DisplayText;
                txtDisplayText.Foreground = System.Windows.Media.Brushes.Black;

                txtSubText.Text = item.SubText;
                bdSubText.Visibility = item.SubTextVisibility;

                lstItems.SelectedItem = item;
            }
            finally
            {
                _isUpdatingInternally = false;
            }
        }

        private void ClearSelectionDisplay()
        {
            _isUpdatingInternally = true;
            try
            {
                SelectedItem = null;
                SelectedValue = null;
                SelectedIndex = -1;
                UpdatePlaceholder();
                bdSubText.Visibility = Visibility.Collapsed;
                lstItems.SelectedItem = null;
            }
            finally
            {
                _isUpdatingInternally = false;
            }
        }

        private void UpdatePlaceholder()
        {
            if (SelectedIndex < 0 && SelectedValue == null)
            {
                txtDisplayText.Text = Placeholder;
                txtDisplayText.Foreground = (System.Windows.Media.Brush)new System.Windows.Media.BrushConverter().ConvertFromString("#94A3B8")!;
            }
        }

        private void BdTrigger_PreviewMouseLeftButtonUp(object sender, MouseButtonEventArgs e)
        {
            // Nếu vừa mới đóng do click ngoài cách đây dưới 250ms thì không mở lại
            if ((DateTime.UtcNow - _lastClosedTime).TotalMilliseconds < 250)
            {
                e.Handled = true;
                return;
            }

            popDropdown.IsOpen = !popDropdown.IsOpen;
            e.Handled = true;
        }

        private void PopDropdown_Opened(object sender, EventArgs e)
        {
            _popupOpenedTime = DateTime.UtcNow;
            IsDropDownOpen = true;

            if (bdPopupContainer != null)
            {
                double triggerWidth = bdTrigger.ActualWidth;
                bdPopupContainer.Width = Math.Max(triggerWidth, 280);
            }

            txtSearch.Text = "";
            txtSearchWatermark.Visibility = Visibility.Visible;
            btnClearSearch.Visibility = Visibility.Collapsed;
            FilterItems("");

            // Đảm bảo focus vào ô tìm kiếm sau khi Popup vẽ xong
            Dispatcher.BeginInvoke(new Action(() =>
            {
                txtSearch.Focus();
                Keyboard.Focus(txtSearch);
            }), System.Windows.Threading.DispatcherPriority.Input);
        }

        private void PopDropdown_Closed(object sender, EventArgs e)
        {
            _lastClosedTime = DateTime.UtcNow;
            IsDropDownOpen = false;
        }

        private void TxtSearch_TextChanged(object sender, TextChangedEventArgs e)
        {
            string keyword = txtSearch.Text;
            bool hasText = !string.IsNullOrEmpty(keyword);
            txtSearchWatermark.Visibility = hasText ? Visibility.Collapsed : Visibility.Visible;
            btnClearSearch.Visibility = hasText ? Visibility.Visible : Visibility.Collapsed;

            FilterItems(keyword.Trim());
        }

        private void FilterItems(string keyword)
        {
            if (string.IsNullOrWhiteSpace(keyword))
            {
                lstItems.ItemsSource = _allItems;
                txtEmptyState.Visibility = Visibility.Collapsed;
                txtResultCount.Text = $"{_allItems.Count} mục khả dụng";
                return;
            }

            string normalizedKeyword = RemoveDiacritics(keyword).ToLowerInvariant();

            var filtered = _allItems.Where(x =>
                x.NormalizedDisplayText.Contains(normalizedKeyword) ||
                x.DisplayText.IndexOf(keyword, StringComparison.OrdinalIgnoreCase) >= 0 ||
                (!string.IsNullOrEmpty(x.SubText) && x.SubText.IndexOf(keyword, StringComparison.OrdinalIgnoreCase) >= 0)
            ).ToList();

            lstItems.ItemsSource = filtered;
            txtEmptyState.Visibility = filtered.Count == 0 ? Visibility.Visible : Visibility.Collapsed;
            txtResultCount.Text = $"Tìm thấy {filtered.Count}/{_allItems.Count}";

            if (filtered.Count > 0)
            {
                lstItems.SelectedIndex = 0;
            }
        }

        private void BtnClearSearch_Click(object sender, RoutedEventArgs e)
        {
            txtSearch.Text = "";
            txtSearch.Focus();
        }

        private void TxtSearch_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Down)
            {
                if (lstItems.Items.Count > 0)
                {
                    lstItems.Focus();
                    if (lstItems.SelectedIndex < 0) lstItems.SelectedIndex = 0;
                    if (lstItems.ItemContainerGenerator.ContainerFromIndex(lstItems.SelectedIndex) is ListBoxItem item)
                    {
                        item.Focus();
                    }
                }
                e.Handled = true;
            }
            else if (e.Key == Key.Enter)
            {
                if (lstItems.SelectedItem is SearchableComboItem selected)
                {
                    SelectAndClose(selected);
                }
                else if (lstItems.Items.Count > 0 && lstItems.Items[0] is SearchableComboItem first)
                {
                    SelectAndClose(first);
                }
                e.Handled = true;
            }
            else if (e.Key == Key.Escape)
            {
                popDropdown.IsOpen = false;
                e.Handled = true;
            }
        }

        private void LstItems_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Enter && lstItems.SelectedItem is SearchableComboItem item)
            {
                SelectAndClose(item);
                e.Handled = true;
            }
            else if (e.Key == Key.Escape)
            {
                popDropdown.IsOpen = false;
                e.Handled = true;
            }
            else if (e.Key == Key.Up && lstItems.SelectedIndex == 0)
            {
                txtSearch.Focus();
                e.Handled = true;
            }
        }

        private void ListBoxItem_PreviewMouseLeftButtonUp(object sender, MouseButtonEventArgs e)
        {
            // Bỏ qua nếu mouse-up xảy ra ngay trong 200ms đầu mở popup (tránh nhầm lẫn thao tác click mở)
            if ((DateTime.UtcNow - _popupOpenedTime).TotalMilliseconds < 200)
            {
                return;
            }

            if (sender is ListBoxItem lbi && lbi.DataContext is SearchableComboItem item)
            {
                SelectAndClose(item);
                e.Handled = true;
            }
        }

        private void SelectAndClose(SearchableComboItem item)
        {
            var oldItem = SelectedItem;
            ApplyItemDisplay(item);
            popDropdown.IsOpen = false;

            // Bắn event SelectionChanged chuẩn WPF
            var removed = oldItem != null ? new List<object> { oldItem } : new List<object>();
            var added = item.RawItem != null ? new List<object> { item.RawItem } : new List<object>();
            RaiseEvent(new SelectionChangedEventArgs(SelectionChangedEvent, removed, added));
        }

        private void RootCtrl_KeyDown(object sender, KeyEventArgs e)
        {
            if ((e.Key == Key.Down || e.Key == Key.Enter || e.Key == Key.Space || e.Key == Key.F4) && !popDropdown.IsOpen)
            {
                popDropdown.IsOpen = true;
                e.Handled = true;
            }
        }

        public static string RemoveDiacritics(string text)
        {
            if (string.IsNullOrWhiteSpace(text)) return "";
            string normalized = text.Normalize(NormalizationForm.FormD);
            var sb = new StringBuilder();
            foreach (char c in normalized)
            {
                var unicodeCategory = CharUnicodeInfo.GetUnicodeCategory(c);
                if (unicodeCategory != UnicodeCategory.NonSpacingMark)
                    sb.Append(c);
            }
            return sb.ToString().Normalize(NormalizationForm.FormC).Replace('đ', 'd').Replace('Đ', 'D');
        }
    }
}
