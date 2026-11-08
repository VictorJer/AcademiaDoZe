using System.Globalization;
using AcademiaDoZe.Application.DTOs;

namespace AcademiaDoZe.Presentation.AppMaui.Converters;

public sealed class FotoToImageSourceConverter : IValueConverter
{
    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
        => value is ArquivoDto { Conteudo.Length: > 0 } arquivo
            ? ImageSource.FromStream(() => new MemoryStream(arquivo.Conteudo))
            : null!;

    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
        => throw new NotSupportedException();
}
