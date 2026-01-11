using System.Globalization;
using System.Windows;
using System.Windows.Data;

namespace SWAI.App.Converters;

/// <summary>
/// Converts null/empty values to Visibility
/// </summary>
public class NullToVisibilityConverter : IValueConverter
{
    /// <summary>
    /// If true, null = Visible, not null = Collapsed
    /// </summary>
    public bool Invert { get; set; }

    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        bool isNullOrEmpty = value == null || 
                            (value is string str && string.IsNullOrEmpty(str));

        if (Invert)
        {
            return isNullOrEmpty ? Visibility.Visible : Visibility.Collapsed;
        }
        
        return isNullOrEmpty ? Visibility.Collapsed : Visibility.Visible;
    }

    public object ConvertBack(object value, Type targetType, object? parameter, CultureInfo culture)
    {
        throw new NotImplementedException();
    }
}

/// <summary>
/// Inverts a boolean value
/// </summary>
public class InverseBooleanConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        if (value is bool b)
            return !b;
        return value;
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
    {
        if (value is bool b)
            return !b;
        return value;
    }
}
