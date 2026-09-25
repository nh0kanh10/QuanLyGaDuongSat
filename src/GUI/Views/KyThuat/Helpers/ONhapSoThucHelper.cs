using System.Text.RegularExpressions;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using DTO.Common;

namespace GUI.Views.KyThuat.Helpers
{
    // =========================================================================
    // O NHAP SO THUC cho cac dialog phan he Ky thuat (Dat).
    //
    // Chi nhan chu so + 1 dau thap phan ('.' hoac ','), khong co dau phan cach
    // hang nghin trong o nhap (FormatHelper.TryParseDecimal hieu "3,950" la 3.95).
    // Roi o thi chuan hoa dinh dang neu so hop le va khong du chu so le, de loi
    // "qua nhieu chu so le" van hien ra cho nguoi dung sua.
    // =========================================================================
    public static class ONhapSoThucHelper
    {
        private static readonly Regex KyTuSoThuc = new(@"^[0-9.,]+$");

        // soChuSoLe: so chu so le toi da theo kieu cot DECIMAL
        // coDinhSoLe: true = luon hien du chu so le (5.0, 0.18); false = bo so 0 thua (3950, 3950.5)
        public static void Gan(TextBox tb, int soChuSoLe, bool coDinhSoLe = true)
        {
            tb.PreviewTextInput += (_, e) =>
            {
                if (!KyTuSoThuc.IsMatch(e.Text)) { e.Handled = true; return; }

                if (e.Text is "." or ",")
                {
                    string conLai = tb.SelectionLength > 0 ? tb.Text.Remove(tb.SelectionStart, tb.SelectionLength) : tb.Text;
                    if (conLai.Contains('.') || conLai.Contains(',')) e.Handled = true;
                }
            };

            tb.PreviewKeyDown += (_, e) => { if (e.Key == Key.Space) e.Handled = true; };

            DataObject.AddPastingHandler(tb, (_, e) =>
            {
                string dan = e.DataObject.GetDataPresent(DataFormats.Text)
                    ? e.DataObject.GetData(DataFormats.Text)?.ToString() ?? ""
                    : "";
                e.CancelCommand();
                if (FormatHelper.TryParseDecimal(dan, out decimal v) && v >= 0)
                {
                    tb.Text = v.ToString(FormatHelper.TechnicalCulture);
                    tb.CaretIndex = tb.Text.Length;
                }
            });

            string mau = soChuSoLe <= 0 ? "0" : "0." + new string(coDinhSoLe ? '0' : '#', soChuSoLe);
            tb.LostFocus += (_, _) =>
            {
                if (FormatHelper.TryParseDecimal(tb.Text, out decimal v) && decimal.Round(v, soChuSoLe) == v)
                    tb.Text = v.ToString(mau, FormatHelper.TechnicalCulture);
            };
        }

        public static decimal? Lay(TextBox tb)
            => FormatHelper.TryParseDecimal(tb.Text, out decimal v) ? v : null;

        public static bool QuaSoChuSoLe(decimal v, int soChuSoLe) => decimal.Round(v, soChuSoLe) != v;
    }
}
