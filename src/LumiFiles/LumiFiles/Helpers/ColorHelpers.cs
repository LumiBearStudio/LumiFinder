using System;
using System.Globalization;
using Windows.UI;

namespace LumiFiles.Helpers
{
    /// <summary>
    /// 코드에서 색상 리터럴을 다룰 때 쓰는 헬퍼.
    /// XAML 은 문자열을 Color 로 자동 변환하지만 코드 경로에는 그런 변환이 없다.
    /// </summary>
    public static class ColorHelpers
    {
        /// <summary>
        /// "#AARRGGBB" 또는 "#RRGGBB" 표기를 Color 로 변환. 잘못된 값은 투명색.
        /// </summary>
        public static Color FromHex(string? hex)
        {
            if (string.IsNullOrWhiteSpace(hex)) return Color.FromArgb(0, 0, 0, 0);

            var s = hex.Trim();
            if (s.StartsWith('#')) s = s[1..];

            try
            {
                if (s.Length == 8)
                {
                    return Color.FromArgb(
                        byte.Parse(s[..2], NumberStyles.HexNumber, CultureInfo.InvariantCulture),
                        byte.Parse(s.Substring(2, 2), NumberStyles.HexNumber, CultureInfo.InvariantCulture),
                        byte.Parse(s.Substring(4, 2), NumberStyles.HexNumber, CultureInfo.InvariantCulture),
                        byte.Parse(s.Substring(6, 2), NumberStyles.HexNumber, CultureInfo.InvariantCulture));
                }
                if (s.Length == 6)
                {
                    return Color.FromArgb(
                        255,
                        byte.Parse(s[..2], NumberStyles.HexNumber, CultureInfo.InvariantCulture),
                        byte.Parse(s.Substring(2, 2), NumberStyles.HexNumber, CultureInfo.InvariantCulture),
                        byte.Parse(s.Substring(4, 2), NumberStyles.HexNumber, CultureInfo.InvariantCulture));
                }
            }
            catch (FormatException)
            {
                // 아래 투명색으로 폴백
            }

            return Color.FromArgb(0, 0, 0, 0);
        }
    }
}
