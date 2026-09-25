using System.Windows;
using System.Windows.Documents;
using System.Windows.Media;
using System.Windows.Media.Imaging;

namespace GUI.Views.KyThuat.Helpers
{
    // =========================================================================
    // "Bong" cua the toa di theo con tro trong luc keo - tha.
    //
    // Chup anh the toa ngay luc bat dau keo (truoc khi the goc bi lam mo),
    // sau do ve lai tai vi tri con tro, nghieng nhe va co bong do de tao cam
    // giac dang "nhac" toa len.
    // =========================================================================
    public class KeoThaAdorner : Adorner
    {
        private readonly ImageBrush _anhThe;
        private readonly Size _kichThuoc;
        private readonly Point _lechTay;      // vi tri con tro ben trong the luc bat dau keo
        private Point _viTri;                 // vi tri con tro trong he toa do cua phan tu nen

        private static readonly Brush BongDo = new SolidColorBrush(Color.FromArgb(55, 15, 23, 42));

        public KeoThaAdorner(UIElement phanTuNen, FrameworkElement theToa, Point lechTay)
            : base(phanTuNen)
        {
            _kichThuoc = new Size(theToa.ActualWidth, theToa.ActualHeight);
            _lechTay = lechTay;
            _anhThe = new ImageBrush(ChupAnh(theToa, _kichThuoc)) { Opacity = 0.92 };

            IsHitTestVisible = false;
        }

        public void CapNhatViTri(Point viTri)
        {
            _viTri = viTri;
            InvalidateVisual();
        }

        protected override void OnRender(DrawingContext dc)
        {
            var goc = new Point(_viTri.X - _lechTay.X, _viTri.Y - _lechTay.Y);
            var khung = new Rect(goc, _kichThuoc);
            var tam = new Point(khung.X + khung.Width / 2, khung.Y + khung.Height / 2);

            dc.PushTransform(new RotateTransform(-2.5, tam.X, tam.Y));
            dc.DrawRoundedRectangle(BongDo, null, new Rect(khung.X + 4, khung.Y + 6, khung.Width, khung.Height), 5, 5);
            dc.DrawRectangle(_anhThe, null, khung);
            dc.Pop();
        }

        // Chup anh qua DrawingVisual de khong bi lech boi Margin / vi tri cua the trong cay giao dien
        private static BitmapSource ChupAnh(FrameworkElement the, Size kichThuoc)
        {
            var dpi = VisualTreeHelper.GetDpi(the);
            int rong = Math.Max(1, (int)Math.Ceiling(kichThuoc.Width * dpi.DpiScaleX));
            int cao = Math.Max(1, (int)Math.Ceiling(kichThuoc.Height * dpi.DpiScaleY));

            var dv = new DrawingVisual();
            using (var dc = dv.RenderOpen())
            {
                dc.DrawRectangle(new VisualBrush(the), null, new Rect(new Point(0, 0), kichThuoc));
            }

            var bmp = new RenderTargetBitmap(rong, cao, dpi.PixelsPerInchX, dpi.PixelsPerInchY, PixelFormats.Pbgra32);
            bmp.Render(dv);
            bmp.Freeze();
            return bmp;
        }
    }
}
