using System.Windows;
using System.Windows.Controls;

namespace GUI.Helpers
{
    // =========================================================================
    // Thanh tieu de / cong cu tu xuong dong khi khung hep (cua so 1100 px).
    //   <Grid helpers:BoCucHepHelper.XuongDongKhiHep="True" helpers:BoCucHepHelper.DoRongTraiToiThieu="300">
    //       ... cac con o hang 0 ...
    //       <StackPanel> cum nut (con CUOI CUNG) </StackPanel>
    //   </Grid>
    // Khi be rong Grid < (be rong cac con phia truoc + be rong con cuoi), con cuoi xuong hang 1, trai het cac cot;
    // du rong lai thi tra ve cho cu. DoRongTraiToiThieu = be rong toi thieu danh cho cac con phia truoc
    // (vd tieu de co TextTrimming); bo trong thi do be rong tu nhien cua chung.
    // =========================================================================
    public static class BoCucHepHelper
    {
        public static readonly DependencyProperty XuongDongKhiHepProperty =
            DependencyProperty.RegisterAttached("XuongDongKhiHep", typeof(bool), typeof(BoCucHepHelper),
                new PropertyMetadata(false, XuongDongKhiHep_Changed));

        public static bool GetXuongDongKhiHep(DependencyObject d) => (bool)d.GetValue(XuongDongKhiHepProperty);
        public static void SetXuongDongKhiHep(DependencyObject d, bool v) => d.SetValue(XuongDongKhiHepProperty, v);

        public static readonly DependencyProperty DoRongTraiToiThieuProperty =
            DependencyProperty.RegisterAttached("DoRongTraiToiThieu", typeof(double), typeof(BoCucHepHelper),
                new PropertyMetadata(0d));

        public static double GetDoRongTraiToiThieu(DependencyObject d) => (double)d.GetValue(DoRongTraiToiThieuProperty);
        public static void SetDoRongTraiToiThieu(DependencyObject d, double v) => d.SetValue(DoRongTraiToiThieuProperty, v);

        // Vi tri goc cua con cuoi (luu lan dau de tra lai)
        private static readonly DependencyProperty ViTriGocProperty =
            DependencyProperty.RegisterAttached("ViTriGoc", typeof(ViTriGoc), typeof(BoCucHepHelper), new PropertyMetadata(null));

        private sealed record ViTriGoc(int Hang, int Cot, int SoCot, Thickness LeNgoai);

        private const double KhoangCach = 12;

        private static void XuongDongKhiHep_Changed(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (d is not Grid g) return;
            g.SizeChanged -= Grid_SizeChanged;
            g.Loaded -= Grid_Loaded;
            if ((bool)e.NewValue)
            {
                g.SizeChanged += Grid_SizeChanged;
                g.Loaded += Grid_Loaded;
            }
        }

        private static readonly DependencyProperty DaGanConProperty =
            DependencyProperty.RegisterAttached("DaGanCon", typeof(bool), typeof(BoCucHepHelper), new PropertyMetadata(false));

        private static void Grid_Loaded(object sender, RoutedEventArgs e)
        {
            var g = (Grid)sender;
            // Con phia truoc doi be rong (vd danh sach ngay nap sau tu CSDL) -> tinh lai
            if (!(bool)g.GetValue(DaGanConProperty))
            {
                g.SetValue(DaGanConProperty, true);
                for (int i = 0; i < g.Children.Count - 1; i++)
                    if (g.Children[i] is FrameworkElement con)
                        con.SizeChanged += (_, ev) => { if (ev.WidthChanged) CapNhat(g); };
            }
            CapNhat(g);
        }
        private static void Grid_SizeChanged(object sender, SizeChangedEventArgs e)
        {
            if (e.WidthChanged) CapNhat((Grid)sender);
        }

        private static void CapNhat(Grid g)
        {
            if (g.ActualWidth <= 0 || g.Children.Count < 2) return;
            if (g.Children[g.Children.Count - 1] is not FrameworkElement cuoi) return;

            if (cuoi.GetValue(ViTriGocProperty) is not ViTriGoc goc)
            {
                goc = new ViTriGoc(Grid.GetRow(cuoi), Grid.GetColumn(cuoi), Grid.GetColumnSpan(cuoi), cuoi.Margin);
                cuoi.SetValue(ViTriGocProperty, goc);
            }

            var vocCung = new Size(double.PositiveInfinity, double.PositiveInfinity);
            cuoi.Measure(vocCung);
            double rongCuoi = cuoi.DesiredSize.Width;

            double rongTrai = GetDoRongTraiToiThieu(g);
            if (rongTrai <= 0)
            {
                for (int i = 0; i < g.Children.Count - 1; i++)
                {
                    if (g.Children[i] is UIElement con && con.Visibility != Visibility.Collapsed)
                    {
                        con.Measure(vocCung);
                        rongTrai += con.DesiredSize.Width;
                    }
                }
            }

            bool hep = g.ActualWidth < rongTrai + rongCuoi + KhoangCach;

            if (hep)
            {
                while (g.RowDefinitions.Count < goc.Hang + 2)
                    g.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
                Grid.SetRow(cuoi, goc.Hang + 1);
                Grid.SetColumn(cuoi, 0);
                Grid.SetColumnSpan(cuoi, System.Math.Max(1, g.ColumnDefinitions.Count));
                cuoi.Margin = new Thickness(0, 8, 0, 0);
            }
            else
            {
                Grid.SetRow(cuoi, goc.Hang);
                Grid.SetColumn(cuoi, goc.Cot);
                Grid.SetColumnSpan(cuoi, goc.SoCot);
                cuoi.Margin = goc.LeNgoai;
            }
        }
    }
}
