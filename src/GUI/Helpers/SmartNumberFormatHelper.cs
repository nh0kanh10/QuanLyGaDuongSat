using System;
using System.Globalization;
using System.Linq;
using System.Text.RegularExpressions;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using DTO.Common;

namespace GUI.Helpers
{
    public static class SmartNumberFormatHelper
    {
        private static readonly Regex _digitOnlyRegex = new(@"^[0-9]+$");
        private static readonly Regex _decimalCharRegex = new(@"^[0-9\.,]+$");

        #region 1. Số nguyên tự động thêm phân cách ngàn (Live Thousands Separator)
        // Gắn bộ lọc và tự động định dạng số nguyên có dấu phẩy phân cách ngàn khi gõ (vd: 50000 -> 50,000).
        // Thích hợp cho: Chiều dài ray (m), tiền tệ, tải trọng nguyên.
       
        public static void AttachIntegerThousandSeparated(TextBox textBox, long min = 0, long max = 100_000_000)
        {
            if (textBox == null) return;

            textBox.PreviewTextInput += (s, e) =>
            {
                // Chỉ cho phép chữ số 0-9
                if (!_digitOnlyRegex.IsMatch(e.Text))
                {
                    e.Handled = true;
                }
            };

            textBox.PreviewKeyDown += (s, e) =>
            {
                // Cấm phím Space
                if (e.Key == Key.Space)
                {
                    e.Handled = true;
                }
            };

            DataObject.AddPastingHandler(textBox, (s, e) =>
            {
                if (e.DataObject.GetDataPresent(DataFormats.Text))
                {
                    string text = e.DataObject.GetData(DataFormats.Text)?.ToString() ?? "";
                    string clean = Regex.Replace(text, @"[^0-9]", "");
                    if (string.IsNullOrEmpty(clean))
                    {
                        e.CancelCommand();
                    }
                    else
                    {
                        e.CancelCommand();
                        // Chèn trực tiếp chuỗi số đã làm sạch
                        int oldCaret = textBox.CaretIndex;
                        textBox.SelectedText = clean;
                        textBox.CaretIndex = oldCaret + clean.Length;
                    }
                }
                else
                {
                    e.CancelCommand();
                }
            });

            bool isFormatting = false;
            textBox.TextChanged += (s, e) =>
            {
                if (isFormatting) return;

                string rawText = textBox.Text;
                if (string.IsNullOrWhiteSpace(rawText)) return;

                // Lấy toàn bộ chữ số
                string digits = Regex.Replace(rawText, @"[^0-9]", "");
                if (string.IsNullOrEmpty(digits))
                {
                    isFormatting = true;
                    textBox.Text = "";
                    isFormatting = false;
                    return;
                }

                if (long.TryParse(digits, out long val))
                {
                    if (val > max) val = max;
                    if (val < min) val = min;

                    string formatted = val.ToString("N0", CultureInfo.InvariantCulture);

                    if (formatted != rawText)
                    {
                        isFormatting = true;

                        // Tính toán vị trí con trỏ chuột dựa theo số lượng chữ số đứng trước con trỏ
                        int caret = textBox.CaretIndex;
                        string textBeforeCaret = rawText.Substring(0, Math.Min(caret, rawText.Length));
                        int digitsBeforeCaret = textBeforeCaret.Count(char.IsDigit);

                        textBox.Text = formatted;

                        // Tìm lại vị trí con trỏ tương ứng trong chuỗi formatted
                        int newCaret = 0;
                        int countedDigits = 0;
                        for (int i = 0; i < formatted.Length; i++)
                        {
                            if (char.IsDigit(formatted[i]))
                            {
                                countedDigits++;
                            }
                            if (countedDigits >= digitsBeforeCaret)
                            {
                                newCaret = i + 1;
                                break;
                            }
                        }
                        if (digitsBeforeCaret == 0) newCaret = 0;

                        textBox.CaretIndex = Math.Min(newCaret, formatted.Length);
                        isFormatting = false;
                    }
                }
            };
        }
        #endregion

        #region 2. Số thực Lý Trình Km (Chặn ký tự lạ, tự format chuẩn x.xx khi LostFocus)
        // Gắn bộ lọc số thực lý trình: chỉ cho phép 0-9 và duy nhất 1 dấu thập phân.
        
        public static void AttachDecimalKm(TextBox textBox, decimal min = 0m, decimal max = 5000m)
        {
            if (textBox == null) return;

            textBox.PreviewTextInput += (s, e) =>
            {
                // Kiểm tra ký tự nhập vào có phải số hoặc dấu chấm/phẩy không
                if (!_decimalCharRegex.IsMatch(e.Text))
                {
                    e.Handled = true;
                    return;
                }

                // Nếu người dùng gõ dấu chấm hoặc phẩy
                if (e.Text == "." || e.Text == ",")
                {
                    // Nếu trong chuỗi hiện tại (trừ phần đang bôi đen) đã có dấu chấm/phẩy rồi thì không cho gõ thêm
                    string curText = textBox.Text;
                    if (textBox.SelectionLength > 0)
                    {
                        curText = curText.Remove(textBox.SelectionStart, textBox.SelectionLength);
                    }

                    if (curText.Contains(".") || curText.Contains(","))
                    {
                        e.Handled = true;
                        return;
                    }
                }
            };

            textBox.PreviewKeyDown += (s, e) =>
            {
                if (e.Key == Key.Space)
                {
                    e.Handled = true;
                }
            };

            DataObject.AddPastingHandler(textBox, (s, e) =>
            {
                if (e.DataObject.GetDataPresent(DataFormats.Text))
                {
                    string pasteText = e.DataObject.GetData(DataFormats.Text)?.ToString() ?? "";
                    if (FormatHelper.TryParseKm(pasteText, out decimal kmVal))
                    {
                        e.CancelCommand();
                        textBox.Text = FormatHelper.FormatKm(kmVal);
                        textBox.CaretIndex = textBox.Text.Length;
                    }
                    else
                    {
                        e.CancelCommand();
                    }
                }
                else
                {
                    e.CancelCommand();
                }
            });

            textBox.LostFocus += (s, e) =>
            {
                string text = textBox.Text.Trim();
                if (string.IsNullOrEmpty(text))
                {
                    textBox.Text = "0.00";
                    return;
                }

                if (FormatHelper.TryParseKm(text, out decimal km))
                {
                    if (km > max) km = max;
                    if (km < min) km = min;
                    textBox.Text = FormatHelper.FormatKm(km);
                }
                else
                {
                    textBox.Text = "0.00";
                }
            };
        }
        #endregion

        #region 3. Số nguyên nhỏ (Số hiệu ray 1..99)
        // Gắn bộ lọc chỉ cho phép số nguyên dương nhỏ (vd: số hiệu đường ray 1..99).
        public static void AttachIntegerSmall(TextBox textBox, int min = 1, int max = 99)
        {
            if (textBox == null) return;

            textBox.PreviewTextInput += (s, e) =>
            {
                if (!_digitOnlyRegex.IsMatch(e.Text))
                {
                    e.Handled = true;
                }
            };

            textBox.PreviewKeyDown += (s, e) =>
            {
                if (e.Key == Key.Space)
                {
                    e.Handled = true;
                }
            };

            DataObject.AddPastingHandler(textBox, (s, e) =>
            {
                if (e.DataObject.GetDataPresent(DataFormats.Text))
                {
                    string pasteText = e.DataObject.GetData(DataFormats.Text)?.ToString() ?? "";
                    string clean = Regex.Replace(pasteText, @"[^0-9]", "");
                    if (int.TryParse(clean, out int val) && val >= min && val <= max)
                    {
                        e.CancelCommand();
                        textBox.Text = val.ToString();
                        textBox.CaretIndex = textBox.Text.Length;
                    }
                    else
                    {
                        e.CancelCommand();
                    }
                }
                else
                {
                    e.CancelCommand();
                }
            });

            textBox.LostFocus += (s, e) =>
            {
                if (int.TryParse(textBox.Text, out int val))
                {
                    if (val < min) val = min;
                    if (val > max) val = max;
                    textBox.Text = val.ToString();
                }
                else
                {
                    textBox.Text = min.ToString();
                }
            };
        }
        #endregion
    }
}
