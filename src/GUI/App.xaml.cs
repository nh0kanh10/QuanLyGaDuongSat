using System.Globalization;
using System.Threading;
using System.Windows;
using System.Windows.Markup;
using Wpf.Ui.Appearance;

namespace GUI
{
    public partial class App : Application
    {
        protected override void OnStartup(StartupEventArgs e)
        {
            AppDomain.CurrentDomain.UnhandledException += (s, args) =>
            {
                var ex = args.ExceptionObject as Exception;
                string msg = $"CRASH AppDomain: {ex?.ToString()}";
                try { System.IO.File.WriteAllText("crash.log", msg); } catch { }
                MessageBox.Show(msg, "Lỗi Khởi Động");
            };

            DispatcherUnhandledException += (s, args) =>
            {
                string msg = $"CRASH Dispatcher: {args.Exception?.ToString()}";
                try { System.IO.File.WriteAllText("crash.log", msg); } catch { }
                MessageBox.Show(msg, "Lỗi Giao Diện");
                args.Handled = true;
            };
            // Thiết lập Culture thống nhất toàn hệ thống Điều Hành Đường Sắt:
            // 1. Kế thừa vi-VN cho ngày tháng, thứ, tên hiển thị ("dd/MM/yyyy")
            // 2. Ép buộc chuẩn kỹ thuật ERP quốc tế cho các trường số (dấu chấm '.' thập phân, dấu phẩy ',' hàng ngàn)
            var appCulture = (CultureInfo)CultureInfo.GetCultureInfo("vi-VN").Clone();
            appCulture.NumberFormat.NumberDecimalSeparator = ".";
            appCulture.NumberFormat.NumberGroupSeparator = ",";
            appCulture.NumberFormat.CurrencyDecimalSeparator = ".";
            appCulture.NumberFormat.CurrencyGroupSeparator = ",";
            appCulture.NumberFormat.PercentDecimalSeparator = ".";
            appCulture.NumberFormat.PercentGroupSeparator = ",";

            CultureInfo.DefaultThreadCurrentCulture = appCulture;
            CultureInfo.DefaultThreadCurrentUICulture = appCulture;
            Thread.CurrentThread.CurrentCulture = appCulture;
            Thread.CurrentThread.CurrentUICulture = appCulture;

            // Đồng bộ toàn bộ các Binding XAML của WPF sang chuẩn số kỹ thuật dấu chấm
            FrameworkElement.LanguageProperty.OverrideMetadata(
                typeof(FrameworkElement),
                new FrameworkPropertyMetadata(XmlLanguage.GetLanguage(CultureInfo.InvariantCulture.IetfLanguageTag)));

            // Nạp cấu hình kết nối CSDL (appsettings.json + appsettings.Local.json)
            // rồi bảo đảm nối được SQL Server trước khi mở cửa sổ đầu tiên.
            // Trước đây chuỗi kết nối viết cứng trong DatabaseHelper nên mỗi máy
            // phải sửa mã nguồn; DamBaoKetNoi() cũng chỉ được gọi trong
            // PhuongTienPage mà app lại mở trang Mạng lưới Ga trước.
            // (Phần Đạt thêm 08/10 — việc nền Sprint 1, KE_HOACH_BACK_END.md §4.1.)
            GUI.Helpers.KetNoiCsdlHelper.DamBaoKetNoi();

            base.OnStartup(e);

            ApplicationThemeManager.Apply(ApplicationTheme.Light);
        }
    }
}
