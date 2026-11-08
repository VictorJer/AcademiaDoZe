using AcademiaDoZe.Application.Enums;
using System.Globalization;

namespace AcademiaDoZe.Presentation.AppMaui.Converters;

public sealed class RestricoesFlagsDisplayConverter : IValueConverter
{
    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value is not AppMatriculaRestricoes flags)
            return string.Empty;
        if (flags == AppMatriculaRestricoes.None)
            return flags.GetDisplayName();

        var nomes = Enum.GetValues<AppMatriculaRestricoes>()
            .Where(item => item != AppMatriculaRestricoes.None && flags.HasFlag(item))
            .Select(item => item.GetDisplayName());
        return string.Join(", ", nomes);
    }

    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
        => throw new NotSupportedException();
}
