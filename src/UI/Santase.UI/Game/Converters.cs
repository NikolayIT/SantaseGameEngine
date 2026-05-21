namespace Santase.UI.Game
{
    using System.Globalization;

    using Microsoft.Maui.Controls;

    public sealed class NotNullConverter : IValueConverter
    {
        public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture) => value != null;

        public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture) => throw new NotSupportedException();
    }

    public sealed class InverseBoolConverter : IValueConverter
    {
        public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
            => !(value is bool b && b);

        public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
            => !(value is bool b && b);
    }

    public sealed class BoolToOpacityConverter : IValueConverter
    {
        public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
            => (value is bool b && b) ? 1.0 : 0.4;

        public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture) => throw new NotSupportedException();
    }

    public sealed class StringNotEmptyConverter : IValueConverter
    {
        public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
            => value is string s && !string.IsNullOrEmpty(s);

        public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture) => throw new NotSupportedException();
    }

    public sealed class BoolToTrumpRotationConverter : IValueConverter
    {
        public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
            => (value is bool b && b) ? 90.0 : 0.0;

        public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture) => throw new NotSupportedException();
    }
}
