using System.Text.Json.Serialization;

namespace Holefeeder.Ui.Common.Models;

[JsonConverter(typeof(JsonStringEnumConverter))]
public enum CategoryType
{
    Expense,
    Gain
}
