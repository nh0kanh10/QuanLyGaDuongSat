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

            base.OnStartup(e);

            ApplicationThemeManager.Apply(ApplicationTheme.Light);
        }
    }
}
