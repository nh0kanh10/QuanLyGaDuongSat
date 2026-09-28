using System.Windows.Media;

namespace GUI.Helpers
{
    /// <summary>
    /// Bộ cọ vẽ chuẩn đóng băng (Frozen Brushes) cho hệ thống VNR.
    /// Giúp WPF bypass toàn bộ dependency tracking và GC allocation, 
    /// đạt hiệu năng vẽ phần cứng tối đa (Hardware Acceleration).
    /// </summary>
    public static class VnrWpfBrushes
    {
        public static readonly SolidColorBrush Navy = CreateFrozen(0x00, 0x3B, 0x73);
        public static readonly SolidColorBrush NavyDark = CreateFrozen(0x00, 0x28, 0x55);
        public static readonly SolidColorBrush NavyDeep = CreateFrozen(0x00, 0x18, 0x38);

        public static readonly SolidColorBrush Sky = CreateFrozen(0x02, 0x84, 0xC7);
        public static readonly SolidColorBrush SkyHover = CreateFrozen(0x03, 0x69, 0xA1);
        public static readonly SolidColorBrush SkyLight = CreateFrozen(0xE0, 0xF2, 0xFE);
        public static readonly SolidColorBrush SkyBorder = CreateFrozen(0xBA, 0xE6, 0xFD);

        public static readonly SolidColorBrush Orange = CreateFrozen(0xEA, 0x58, 0x0C);
        public static readonly SolidColorBrush OrangeBorder = CreateFrozen(0xC2, 0x41, 0x0C);
        public static readonly SolidColorBrush OrangeLight = CreateFrozen(0xFF, 0xF7, 0xED);
        public static readonly SolidColorBrush OrangeHeadrest = CreateFrozen(0xFE, 0xD7, 0xAA);

        public static readonly SolidColorBrush Green = CreateFrozen(0x16, 0xA3, 0x4A);
        public static readonly SolidColorBrush GreenDark = CreateFrozen(0x15, 0x80, 0x3D);
        public static readonly SolidColorBrush GreenBg = CreateFrozen(0xEC, 0xFD, 0xF5);
        public static readonly SolidColorBrush GreenBorder = CreateFrozen(0xA7, 0xF3, 0xD0);

        public static readonly SolidColorBrush Red = CreateFrozen(0xDC, 0x26, 0x26);
        public static readonly SolidColorBrush RedDark = CreateFrozen(0xB9, 0x1C, 0x1C);
        public static readonly SolidColorBrush RedBg = CreateFrozen(0xFE, 0xF2, 0xF2);
        public static readonly SolidColorBrush RedBorder = CreateFrozen(0xFE, 0xCA, 0xCA);

        public static readonly SolidColorBrush SlateCanvas = CreateFrozen(0xF1, 0xF5, 0xF9);
        public static readonly SolidColorBrush SlateBg = CreateFrozen(0xF8, 0xFA, 0xFC);
        public static readonly SolidColorBrush SlateLight = CreateFrozen(0xE2, 0xE8, 0xF0);
        public static readonly SolidColorBrush SlateBorder = CreateFrozen(0xCB, 0xD5, 0xE1);

        public static readonly SolidColorBrush TextDark = CreateFrozen(0x0F, 0x17, 0x2A);
        public static readonly SolidColorBrush TextMuted = CreateFrozen(0x47, 0x55, 0x69);
        public static readonly SolidColorBrush TextLightMuted = CreateFrozen(0x64, 0x74, 0x8B);
        public static readonly SolidColorBrush TextDisabled = CreateFrozen(0x94, 0xA3, 0xB8);

        public static readonly SolidColorBrush HeadrestDefault = CreateFrozen(0xE2, 0xE8, 0xF0);

        private static SolidColorBrush CreateFrozen(byte r, byte g, byte b)
        {
            var brush = new SolidColorBrush(Color.FromRgb(r, g, b));
            brush.Freeze();
            return brush;
        }
    }
}
