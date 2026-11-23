using System.Globalization;
using System.Text.Json;
using Serilog.Events;
using Serilog.Formatting;
using Serilog.Formatting.Json;

namespace Fundoo.Observability;

/// <summary>
/// Writes each Serilog event as indented JSON for easier manual inspection.
/// </summary>
public sealed class IndentedJsonFormatter : ITextFormatter
{
    private static readonly JsonSerializerOptions IndentedOptions = new()
    {
        WriteIndented = true
    };

    private readonly JsonFormatter _innerFormatter = new();

    public void Format(LogEvent logEvent, TextWriter output)
    {
        using var buffer = new StringWriter(CultureInfo.InvariantCulture);
        _innerFormatter.Format(logEvent, buffer);

        using var document = JsonDocument.Parse(buffer.ToString());
        output.Write(JsonSerializer.Serialize(document.RootElement, IndentedOptions));
        output.WriteLine();
    }
}
