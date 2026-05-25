using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using CsvHelper;
using CsvHelper.Configuration;
using SerialPlot.Models;

namespace SerialPlot.Services;

public sealed class CsvStreamParser(TimestampUnit timestampUnit)
{
    private readonly CsvConfiguration _csvConfig = new(CultureInfo.InvariantCulture)
    {
        HasHeaderRecord = false,
        BadDataFound = null,
        MissingFieldFound = null,
        TrimOptions = TrimOptions.None,
    };

    public CsvSchema ParseHeader(string line)
    {
        var fields = ParseFields(line);
        if (fields.Count == 0)
        {
            throw new CsvSchemaException("CSV header is empty.");
        }

        if (fields.Any(string.IsNullOrWhiteSpace))
        {
            throw new CsvSchemaException("CSV header contains a blank column name.");
        }

        var duplicates = fields.GroupBy(x => x, StringComparer.Ordinal).Where(x => x.Count() > 1).Select(x => x.Key).ToArray();
        if (duplicates.Length > 0)
        {
            throw new CsvSchemaException($"CSV header contains duplicate column name '{duplicates[0]}'.");
        }

        return new CsvSchema(fields);
    }

    public ParsedRow ParseRow(CsvSchema schema, string line)
    {
        var fields = ParseFields(line);
        if (fields.Count != schema.Headers.Count)
        {
            throw new CsvSchemaException($"CSV row has {fields.Count} columns but header has {schema.Headers.Count}.");
        }

        var cells = fields.Select(ParseCell).ToArray();
        return new ParsedRow(fields, cells, line);
    }

    private IReadOnlyList<string> ParseFields(string line)
    {
        using var reader = new StringReader(line);
        using var csv = new CsvReader(reader, _csvConfig);
        if (!csv.Read())
        {
            return Array.Empty<string>();
        }

        var record = csv.Parser.Record;
        return record?.ToArray() ?? Array.Empty<string>();
    }

    private ParsedCell ParseCell(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return ParsedCell.Gap;
        }

        if (double.TryParse(value, NumberStyles.Float | NumberStyles.AllowThousands, CultureInfo.InvariantCulture, out var invariantNumber)
            || double.TryParse(value, NumberStyles.Float | NumberStyles.AllowThousands, CultureInfo.CurrentCulture, out invariantNumber))
        {
            if (TryUnixTimestamp(invariantNumber, out var unixDate))
            {
                return new ParsedCell(unixDate.ToUnixTimeMilliseconds(), unixDate, true);
            }

            return new ParsedCell(invariantNumber, null, true);
        }

        if (DateTimeOffset.TryParse(
                value,
                CultureInfo.InvariantCulture,
                DateTimeStyles.AssumeLocal | DateTimeStyles.AllowWhiteSpaces,
                out var invariantDate)
            || DateTimeOffset.TryParse(
                value,
                CultureInfo.CurrentCulture,
                DateTimeStyles.AssumeLocal | DateTimeStyles.AllowWhiteSpaces,
                out invariantDate))
        {
            return new ParsedCell(invariantDate.ToUnixTimeMilliseconds(), invariantDate, true);
        }

        return ParsedCell.Gap;
    }

    private bool TryUnixTimestamp(double value, out DateTimeOffset dateTime)
    {
        var absolute = Math.Abs(value);
        var unit = timestampUnit;
        if (unit == TimestampUnit.Auto)
        {
            unit = absolute switch
            {
                >= 1e17 => TimestampUnit.Nanoseconds,
                >= 1e14 => TimestampUnit.Microseconds,
                >= 1e11 => TimestampUnit.Milliseconds,
                >= 1e8 => TimestampUnit.Seconds,
                _ => TimestampUnit.Auto,
            };
        }
        else if (!IsPlausibleUnixTimestamp(absolute, unit))
        {
            dateTime = default;
            return false;
        }

        try
        {
            dateTime = unit switch
            {
                TimestampUnit.Seconds => DateTimeOffset.FromUnixTimeMilliseconds(checked((long)Math.Round(value * 1_000d))),
                TimestampUnit.Milliseconds => DateTimeOffset.FromUnixTimeMilliseconds(checked((long)Math.Round(value))),
                TimestampUnit.Microseconds => DateTimeOffset.FromUnixTimeMilliseconds(checked((long)Math.Round(value / 1_000d))),
                TimestampUnit.Nanoseconds => DateTimeOffset.FromUnixTimeMilliseconds(checked((long)Math.Round(value / 1_000_000d))),
                _ => default,
            };
            return unit != TimestampUnit.Auto;
        }
        catch (ArgumentOutOfRangeException)
        {
            dateTime = default;
            return false;
        }
        catch (OverflowException)
        {
            dateTime = default;
            return false;
        }
    }

    private static bool IsPlausibleUnixTimestamp(double absolute, TimestampUnit unit) => unit switch
    {
        TimestampUnit.Seconds => absolute >= 1e8,
        TimestampUnit.Milliseconds => absolute >= 1e11,
        TimestampUnit.Microseconds => absolute >= 1e14,
        TimestampUnit.Nanoseconds => absolute >= 1e17,
        _ => false,
    };
}

public sealed class CsvSchemaException(string message) : Exception(message);
