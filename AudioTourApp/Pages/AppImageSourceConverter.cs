using System.Globalization;

namespace AudioTourApp.Pages;

internal sealed class AppImageSourceConverter : IValueConverter
{
    public static AppImageSourceConverter Instance { get; } = new();

    public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        var raw = value?.ToString()?.Trim();
        if (string.IsNullOrWhiteSpace(raw))
        {
            return null;
        }

        if (Uri.TryCreate(raw, UriKind.Absolute, out var absolute))
        {
            if (absolute.IsFile)
            {
                return ImageSource.FromFile(absolute.LocalPath);
            }

            if (absolute.Scheme.Equals(Uri.UriSchemeHttp, StringComparison.OrdinalIgnoreCase) ||
                absolute.Scheme.Equals(Uri.UriSchemeHttps, StringComparison.OrdinalIgnoreCase))
            {
                return ImageSource.FromUri(absolute);
            }
        }

        if (Path.IsPathRooted(raw) ||
            raw.StartsWith("/data/", StringComparison.OrdinalIgnoreCase) ||
            raw.StartsWith("/storage/", StringComparison.OrdinalIgnoreCase))
        {
            return ImageSource.FromFile(raw);
        }

        return ImageSource.FromFile(raw);
    }

    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
        => value?.ToString() ?? string.Empty;
}
