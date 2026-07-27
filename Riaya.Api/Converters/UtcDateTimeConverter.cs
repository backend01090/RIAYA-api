using System.Text.Json;
using System.Text.Json.Serialization;

namespace Riaya.Api.Converters;

/// <summary>
/// Forces every <see cref="DateTime"/> arriving in a request body to
/// <see cref="DateTimeKind.Utc"/>.
///
/// The appointment and visit columns are <c>timestamp with time zone</c>, and
/// Npgsql refuses to write a <see cref="DateTime"/> whose Kind is Unspecified.
/// Clients post ISO-8601 without an offset ("2026-07-27T15:45:00.000"), which
/// System.Text.Json binds as Unspecified, so any handler that put such a value
/// into a query or saved it threw ArgumentException and surfaced as a 500 —
/// appointment creation being the visible case.
///
/// Labelling the incoming wall-clock as UTC rather than converting it keeps the
/// stored value equal to what the user picked, which is the convention the
/// seeder already follows (DemoDataSeeder uses SpecifyKind on DateTime.Today)
/// and what the clients render back without any timezone conversion.
/// </summary>
public sealed class UtcDateTimeConverter : JsonConverter<DateTime>
{
    public override DateTime Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        var value = reader.GetDateTime();
        return value.Kind switch
        {
            DateTimeKind.Utc => value,
            // A value that carried an offset is already an absolute instant.
            DateTimeKind.Local => value.ToUniversalTime(),
            _ => DateTime.SpecifyKind(value, DateTimeKind.Utc),
        };
    }

    public override void Write(Utf8JsonWriter writer, DateTime value, JsonSerializerOptions options)
        => writer.WriteStringValue(DateTime.SpecifyKind(value, DateTimeKind.Utc));
}

/// <summary>Nullable companion to <see cref="UtcDateTimeConverter"/>.</summary>
public sealed class UtcNullableDateTimeConverter : JsonConverter<DateTime?>
{
    private static readonly UtcDateTimeConverter Inner = new();

    public override DateTime? Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
        => reader.TokenType == JsonTokenType.Null ? null : Inner.Read(ref reader, typeof(DateTime), options);

    public override void Write(Utf8JsonWriter writer, DateTime? value, JsonSerializerOptions options)
    {
        if (value is null)
        {
            writer.WriteNullValue();
            return;
        }

        Inner.Write(writer, value.Value, options);
    }
}
