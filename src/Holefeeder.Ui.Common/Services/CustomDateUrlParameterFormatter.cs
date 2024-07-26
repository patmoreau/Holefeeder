using System.Globalization;
using System.Reflection;

using Refit;

namespace Holefeeder.Ui.Common.Services;

public class CustomDateUrlParameterFormatter : IUrlParameterFormatter
{
    public string? Format(object? value, ICustomAttributeProvider attributeProvider, Type type)
    {
        if (value is DateOnly dt)
        {
            return dt.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture);
        }

        return value?.ToString();
    }
}
