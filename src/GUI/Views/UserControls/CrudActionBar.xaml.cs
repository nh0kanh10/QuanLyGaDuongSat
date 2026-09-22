using System.Windows;
using System.Windows.Controls;
using Wpf.Ui.Controls;

namespace GUI.Views.UserControls
{
    public partial class CrudActionBar : UserControl
    {
        public event RoutedEventHandler? SaveClicked;
        public event RoutedEventHandler? CancelClicked;
        public event RoutedEventHandler? DeleteClicked;

        public CrudActionBar()
        {
            InitializeComponent();
        }

        public void SetSaveMode(bool isAddingNew)
        {
            if (isAddingNew)
            {
                txtLuu.Text = "Lưu Ga Mới";
                iconLuu.Symbol = SymbolRegular.AddCircle24;
            }
            else
            {
                txtLuu.Text = "Lưu Thay Đổi";
                iconLuu.Symbol = SymbolRegular.Save24;
            }
        }

        public void SetDeleteEnabled(bool isEnabled)
        {
            btnXoa.IsEnabled = isEnabled;
        }

        public void SetSaveEnabled(bool isEnabled)
        {
            btnLuu.IsEnabled = isEnabled;
            btnLuu.ToolTip = isEnabled 
                ? "Lưu các thông tin đã chỉnh sửa vào CSDL (Phím tắt: Ctrl + S)" 
                : "Chưa có thay đổi nào cần lưu";
        }

        public void SetCancelEnabled(bool isEnabled)
        {
            btnHuy.IsEnabled = isEnabled;
        }

        private void BtnLuu_Click(object sender, RoutedEventArgs e) => SaveClicked?.Invoke(this, e);
        private void BtnHuy_Click(object sender, RoutedEventArgs e) => CancelClicked?.Invoke(this, e);
        private void BtnXoa_Click(object sender, RoutedEventArgs e) => DeleteClicked?.Invoke(this, e);
    }
}
