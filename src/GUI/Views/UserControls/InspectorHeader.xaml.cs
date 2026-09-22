using System.Windows.Controls;
using System.Windows.Media;
using Wpf.Ui.Controls;

namespace GUI.Views.UserControls
{
    public partial class InspectorHeader : UserControl
    {
        private int _currentMaGa = 0;
        private bool _isCreateMode = false;

        public InspectorHeader()
        {
            InitializeComponent();
        }

        public void SetStationDetail(string tenGa, string maCode, int maGa, string tuyen)
        {
            _isCreateMode = false;
            _currentMaGa = maGa;
            txtTitle.Text = $"GA: {tenGa.ToUpper()} ({maCode})";
            txtSubtitle.Text = $"Mã Ga: {maCode}  •  {tuyen}";
            SetDirtyState(false);
            iconHeader.Symbol = SymbolRegular.Location24;
            iconHeader.Foreground = new SolidColorBrush(Color.FromRgb(0x00, 0x3B, 0x73));
        }

        public void SetCreateMode()
        {
            _isCreateMode = true;
            _currentMaGa = 0;
            txtTitle.Text = "KHAI BÁO GA MỚI";
            txtSubtitle.Text = "Nhập thông tin ga và bấm [Lưu Ga Mới] để lưu";
            txtBadge.Text = "THÊM MỚI";
            badgeContainer.Background = new SolidColorBrush(Color.FromRgb(0xEE, 0xF2, 0xF6));
            badgeContainer.BorderBrush = new SolidColorBrush(Color.FromRgb(0x00, 0x3B, 0x73));
            txtBadge.Foreground = new SolidColorBrush(Color.FromRgb(0x00, 0x3B, 0x73));
            iconHeader.Symbol = SymbolRegular.AddCircle24;
            iconHeader.Foreground = new SolidColorBrush(Color.FromRgb(0x00, 0x3B, 0x73));
        }

        public void SetDirtyState(bool isDirty)
        {
            if (_isCreateMode) return;

            if (isDirty)
            {
                txtBadge.Text = "● Chưa lưu";
                badgeContainer.Background = new SolidColorBrush(Color.FromRgb(0xFE, 0xF3, 0xC7));
                badgeContainer.BorderBrush = new SolidColorBrush(Color.FromRgb(0xF5, 0x9E, 0x0B));
                txtBadge.Foreground = new SolidColorBrush(Color.FromRgb(0xB4, 0x53, 0x09));
            }
            else
            {
                txtBadge.Text = $"MÃ #{_currentMaGa}";
                badgeContainer.Background = new SolidColorBrush(Color.FromRgb(0xF8, 0xFA, 0xFC));
                badgeContainer.BorderBrush = new SolidColorBrush(Color.FromRgb(0xE2, 0xE8, 0xF0));
                txtBadge.Foreground = new SolidColorBrush(Color.FromRgb(0x64, 0x74, 0x8B));
            }
        }
    }
}
