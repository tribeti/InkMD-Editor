using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Data;
using System;

namespace InkMD_Editor.Converters;

public class DoubleToIntConverter : IValueConverter
{
    public object Convert (object value , Type targetType , object parameter , string language)
    {
        if ( value is null )
            return 14;

        if ( value is double doubleValue )
            return (int) Math.Round(doubleValue);

        if ( value is int intValue )
            return intValue;

        if ( double.TryParse(value.ToString() , out double parsedDouble) )
            return (int) Math.Round(parsedDouble);

        return DependencyProperty.UnsetValue;
    }

    public object ConvertBack (object value , Type targetType , object parameter , string language)
    {
        if ( value is null )
            return 14.0;

        if ( value is int intValue )
            return (double) intValue;

        if ( value is double doubleValue )
            return doubleValue;

        if ( int.TryParse(value.ToString() , out int parsedInt) )
            return (double) parsedInt;

        return 14.0;
    }
}
