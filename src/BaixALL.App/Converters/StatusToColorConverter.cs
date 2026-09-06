using System;
using System.Globalization;
using System.Windows.Data;
using System.Windows.Media;
using BaixALL.App.Models;

namespace BaixALL.App.Converters;

public class StatusToColorConverter : IValueConverter
{
    private static readonly SolidColorBrush BlueBrush = new(Color.FromRgb(0, 120, 215));
    private static readonly SolidColorBrush GreenBrush = new(Color.FromRgb(16, 124, 65));
    private static readonly SolidColorBrush RedBrush = new(Color.FromRgb(216, 59, 1));
    private static readonly SolidColorBrush GrayBrush = new(Color.FromRgb(128, 128, 128));
    private static readonly SolidColorBrush YellowBrush = new(Color.FromRgb(202, 138, 4));

    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value is DownloadStatus status)
        {
            return status switch
            {
                DownloadStatus.Completed => GreenBrush,
                DownloadStatus.Error => RedBrush,
                DownloadStatus.Canceled => GrayBrush,
                DownloadStatus.Queued => YellowBrush,
                _ => BlueBrush
            };
        }

        return GrayBrush;
    }

    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture) => throw new NotImplementedException();
}
